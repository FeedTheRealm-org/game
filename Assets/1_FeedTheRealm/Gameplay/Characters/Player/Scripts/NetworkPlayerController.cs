using UnityEngine;
using Mirror;

/// <summary>
/// Handles networked player input, connecting to MovementComponent and DashComponent.
/// This replaces the need for MP-specific versions by keeping components modular.
/// </summary>
public class NetworkPlayerController : Mirror.NetworkBehaviour {
    [Header("Input")]
    private PlayerControls playerControls;
    private CharacterStateMachine stateMachine;

    [SerializeField] private Logging.Logger logger;
    [SerializeField] private PlayerInputReader playerInputReader;

    public override void OnStartAuthority() {
        logger.Log($"NetworkPlayerController.OnStartAuthority - netId: {netId}", this);

        // Initialize the inventory first (for both local and remote)
        var inventoryReference = GetComponent<PlayerInventoryReference>();
        if (inventoryReference != null) {
            inventoryReference.InitializeForNetworkedPlayer();
        }

        // Only initialize input for the local player
        if (isLocalPlayer) {
            logger.Log($"NetworkPlayerController initialized for LOCAL player {netId}", this);
            InitializeInputWithReader();
        }
    }

    private void InitializeInputWithReader() {
        if (!isLocalPlayer) return;

        stateMachine = GetComponentInChildren<CharacterStateMachine>();
        if (stateMachine == null) {
            logger.Log($"CharacterStateMachine not found for player {netId}", this, Logging.LogType.Error);
            return;
        }

        // Subscribe to PlayerInputReader events instead of creating new PlayerControls
        playerInputReader.MoveEvent += OnMoveInput;
        playerInputReader.DashEvent += OnDashInput;
        playerInputReader.AttackEvent += OnAttackInput;

        logger.Log($"Input configured using PlayerInputReader for player {netId}", this);
    }

    // Methods for PlayerInputReader events (when using shared input)
    private void OnMoveInput(Vector2 direction) {
        if (Cursor.visible) {
            return;
        }
        stateMachine?.OnMove(direction);
    }

    private void OnDashInput() {
        if (Cursor.visible) {
            return;
        }
        stateMachine?.OnDash();
    }

    private void OnAttackInput() {
        if (Cursor.visible) {
            logger.Log("Attack blocked - Cursor is visible", this);
            return;
        }
        stateMachine?.OnAttack();
    }

    public override void OnStopAuthority() {
        CleanupInput();
    }

    private void OnDestroy() {
        // Cleanup in case OnStopAuthority wasn't called
        CleanupInput();
    }

    private void CleanupInput() {
        if (playerInputReader != null) {
            playerInputReader.MoveEvent -= OnMoveInput;
            playerInputReader.DashEvent -= OnDashInput;
            playerInputReader.AttackEvent -= OnAttackInput;
        }
    }

    /// <summary>
    /// Command called by the client to request that the server despawn a collected loot
    /// </summary>
    [Command]
    public void CmdRequestDespawnLoot(uint lootNetworkId) {
        logger?.Log($"[NetworkPlayerController] Client requests despawn of loot netId={lootNetworkId}", this);

        // Search for the NetworkIdentity by ID in Mirror's spawned dictionary
        if (NetworkServer.spawned.TryGetValue(lootNetworkId, out NetworkIdentity lootIdentity)) {
            if (lootIdentity != null) {
                logger?.Log($"[NetworkPlayerController] Despawning loot netId={lootNetworkId}", this);
                NetworkServer.Destroy(lootIdentity.gameObject);
            } else {
                logger?.Log($"[NetworkPlayerController] NetworkIdentity {lootNetworkId} is null", this);
            }
        } else {
            logger?.Log($"[NetworkPlayerController] NetworkIdentity {lootNetworkId} not found in spawned", this);
        }
    }
}
