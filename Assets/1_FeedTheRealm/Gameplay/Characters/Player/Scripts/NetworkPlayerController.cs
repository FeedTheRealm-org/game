using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

/// <summary>
/// Handles networked player input, connecting to MovementComponent and DashComponent.
/// This replaces the need for MP-specific versions by keeping components modular.
/// </summary>
public class NetworkPlayerController : Mirror.NetworkBehaviour {
    [Header("Input")]
    private PlayerControls playerControls;
    private MovementComponent movementComponent;
    private DashComponent dashComponent;
    private AttackComponent attackComponent;

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

            // Check if playerInputReader is assigned before creating PlayerControls
            if (playerInputReader != null) {
                logger.Log("Using PlayerInputReader for input (shared with GameSceneManager)", this);
                InitializeInputWithReader();
            } else {
                logger.Log("PlayerInputReader not assigned, creating standalone PlayerControls", this, Logging.LogType.Warning);
                InitializeInput();
            }
        }
    }

    private void InitializeInputWithReader() {
        if (!isLocalPlayer) return;

        // Search for components
        movementComponent = GetComponentInChildren<MovementComponent>();
        if (movementComponent == null) {
            logger.Log($"MovementComponent not found for player {netId}", this, Logging.LogType.Error);
            return;
        }

        dashComponent = GetComponentInChildren<DashComponent>();
        if (dashComponent == null) {
            logger.Log($"DashComponent not found for player {netId}", this, Logging.LogType.Error);
        }

        attackComponent = GetComponentInChildren<AttackComponent>();
        if (attackComponent == null) {
            logger.Log($"AttackComponent not found for player {netId}", this, Logging.LogType.Error);
        }

        // Subscribe to PlayerInputReader events instead of creating new PlayerControls
        playerInputReader.MoveEvent += OnMoveInput;
        playerInputReader.DashEvent += OnDashInput;
        playerInputReader.AttackEvent += OnAttackInput;

        logger.Log($"Input configured using PlayerInputReader for player {netId}", this);
    }

    private void InitializeInput() {
        if (!isLocalPlayer) return;

        // Search for components in children as well
        movementComponent = GetComponentInChildren<MovementComponent>();
        if (movementComponent == null) {
            logger.Log($"MovementComponent not found for player {netId}", this, Logging.LogType.Error);
            return;
        }

        dashComponent = GetComponentInChildren<DashComponent>();
        if (dashComponent == null) {
            logger.Log($"DashComponent not found for player {netId}", this, Logging.LogType.Error);
        }

        attackComponent = GetComponentInChildren<AttackComponent>();
        if (attackComponent == null) {
            logger.Log($"AttackComponent not found for player {netId}", this, Logging.LogType.Error);
        }

        // Create player-specific controls
        playerControls = new PlayerControls();
        playerControls.Player.Enable();

        // Configure input callbacks - no lambdas to avoid unsubscription issues
        playerControls.Player.Move.performed += OnMovePerformed;
        playerControls.Player.Move.canceled += OnMoveCanceled;
        playerControls.Player.Dash.performed += OnDashPerformed;
        playerControls.Player.Attack.performed += OnAttackPerformed;

        logger.Log($"Input configured for player {netId}", this);
    }

    // Methods for PlayerInputReader events (when using shared input)
    private void OnMoveInput(Vector2 direction) {
        if (Cursor.visible) {
            return;
        }
        movementComponent?.OnMove(direction);
    }

    private void OnDashInput() {
        if (Cursor.visible) {
            return;
        }
        dashComponent?.OnDash();
    }

    private void OnAttackInput() {
        if (Cursor.visible) {
            logger.Log("Attack blocked - Cursor is visible", this);
            return;
        }
        attackComponent?.OnAttack();
    }

    // Separate methods to avoid lambda issues (when using standalone PlayerControls)
    private void OnMovePerformed(InputAction.CallbackContext context) {
        Vector2 direction = context.ReadValue<Vector2>();
        movementComponent?.OnMove(direction);
    }

    private void OnMoveCanceled(InputAction.CallbackContext context) {
        movementComponent?.OnMove(Vector2.zero);
    }

    private void OnDashPerformed(InputAction.CallbackContext context) {
        dashComponent?.OnDash();
    }

    private void OnAttackPerformed(InputAction.CallbackContext context) {
        if (Cursor.visible) {
            logger.Log("Attack blocked - Cursor is visible", this);
            return;
        }

        attackComponent?.OnAttack();
    }

    public override void OnStopAuthority() {
        CleanupInput();
    }

    private void OnDestroy() {
        // Cleanup in case OnStopAuthority wasn't called
        CleanupInput();
    }

    private void CleanupInput() {
        // Cleanup PlayerInputReader events if using shared input
        if (playerInputReader != null) {
            playerInputReader.MoveEvent -= OnMoveInput;
            playerInputReader.DashEvent -= OnDashInput;
            playerInputReader.AttackEvent -= OnAttackInput;
        }

        // Cleanup standalone PlayerControls if they were created
        if (playerControls != null) {
            playerControls.Player.Disable();
            playerControls.Player.Move.performed -= OnMovePerformed;
            playerControls.Player.Move.canceled -= OnMoveCanceled;
            playerControls.Player.Dash.performed -= OnDashPerformed;
            playerControls.Player.Attack.performed -= OnAttackPerformed;
            playerControls.Dispose();
            playerControls = null;
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
