using Mirror;
using UnityEngine;

/// <summary>
/// Handles networked player input, connecting to MovementComponent and DashComponent.
/// This replaces the need for MP-specific versions by keeping components modular.
/// Also supports local testing without network.
/// </summary>
public class NetworkPlayerController : Mirror.NetworkBehaviour
{
    [Header("Input")]
    [SerializeField]
    private PlayerInputReader playerInputReader;

    [SerializeField]
    private Logging.Logger logger;

    private CharacterStateMachine _stateMachine;

    private void Start()
    {
        // Support for non-networked scenes (local testing)
        if (!NetworkClient.active && !NetworkServer.active)
        {
            logger.Log(
                "NetworkPlayerController: Non-networked mode detected, initializing as local player",
                this
            );

            var inventoryReference = GetComponent<PlayerInventoryReference>();
            inventoryReference?.InitializeForNetworkedPlayer();

            RegisterCallbacks();
        }
    }

    public override void OnStartAuthority()
    {
        logger.Log($"NetworkPlayerController.OnStartAuthority - netId: {netId}", this);

        // Initialize the inventory first (for both local and remote)
        var inventoryReference = GetComponent<PlayerInventoryReference>();
        if (inventoryReference != null)
        {
            inventoryReference.InitializeForNetworkedPlayer();
        }

        // Only initialize input for the local player
        if (isLocalPlayer)
        {
            logger.Log($"NetworkPlayerController initialized for LOCAL player {netId}", this);
            RegisterCallbacks();
        }
    }

    public override void OnStopAuthority()
    {
        UnregisterCallbacks();
    }

    private void OnDestroy()
    {
        UnregisterCallbacks();
    }

    private void RegisterCallbacks()
    {
        _stateMachine = GetComponentInChildren<CharacterStateMachine>();
        if (_stateMachine == null)
        {
            logger.Log(
                $"CharacterStateMachine not found for player {netId}",
                this,
                Logging.LogType.Error
            );
            return;
        }

        playerInputReader.MoveEvent += OnMoveInput;
        playerInputReader.DashEvent += OnDashInput;
        playerInputReader.AttackEvent += OnAttackInput;
        playerInputReader.InteractEvent += OnInteractInput;

        logger.Log($"Input configured using PlayerInputReader for player {netId}", this);
    }

    private void UnregisterCallbacks()
    {
        if (playerInputReader == null)
            return;
        playerInputReader.MoveEvent -= OnMoveInput;
        playerInputReader.DashEvent -= OnDashInput;
        playerInputReader.AttackEvent -= OnAttackInput;
        playerInputReader.InteractEvent -= OnInteractInput;
    }

    /* --- Input callback handlers --- */

    private void OnMoveInput(Vector2 direction)
    {
        if (Cursor.visible)
            return;
        _stateMachine?.OnMove(direction);
    }

    private void OnDashInput()
    {
        if (Cursor.visible)
            return;
        _stateMachine?.OnDash();
    }

    private void OnAttackInput()
    {
        if (Cursor.visible)
            return;
        _stateMachine?.OnAttack();
    }

    private void OnInteractInput()
    {
        if (Cursor.visible)
            return;
        _stateMachine?.OnInteract();
    }

    /// <summary>
    /// Command called by the client to request that the server despawn a collected loot
    /// </summary>
    [Command]
    public void CmdRequestDespawnLoot(uint lootNetworkId)
    {
        logger?.Log(
            $"[NetworkPlayerController] Client requests despawn of loot netId={lootNetworkId}",
            this
        );

        // Search for the NetworkIdentity by ID in Mirror's spawned dictionary
        if (NetworkServer.spawned.TryGetValue(lootNetworkId, out NetworkIdentity lootIdentity))
        {
            if (lootIdentity != null)
            {
                logger?.Log(
                    $"[NetworkPlayerController] Despawning loot netId={lootNetworkId}",
                    this
                );
                NetworkServer.Destroy(lootIdentity.gameObject);
            }
            else
            {
                logger?.Log(
                    $"[NetworkPlayerController] NetworkIdentity {lootNetworkId} is null",
                    this
                );
            }
        }
        else
        {
            logger?.Log(
                $"[NetworkPlayerController] NetworkIdentity {lootNetworkId} not found in spawned",
                this
            );
        }
    }
}
