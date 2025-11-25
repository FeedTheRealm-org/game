using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

/// <summary>
/// Handles networked player input, connecting to MovementComponent and DashComponent.
/// This replaces the need for MP-specific versions by keeping components modular.
/// </summary>
public class NetworkPlayerController : NetworkBehaviour {
    [Header("Input")]
    private PlayerControls playerControls;
    private MovementComponent movementComponent;
    private DashComponent dashComponent;
    private AttackComponent attackComponent;

    [SerializeField] private Logging.Logger logger;
    [SerializeField] private PlayerInputReader playerInputReader;

    public override void OnNetworkSpawn() {
        logger.Log($"NetworkPlayerController.OnNetworkSpawn - IsOwner: {IsOwner}, ClientId: {OwnerClientId}", this);

        // Inicializar el inventario primero (tanto para local como remoto)
        var inventoryReference = GetComponent<PlayerInventoryReference>();
        if (inventoryReference != null) {
            inventoryReference.InitializeForNetworkedPlayer();
        }

        // Solo inicializar input para el jugador local
        if (IsOwner) {
            logger.Log($"NetworkPlayerController initialized for LOCAL player {OwnerClientId}", this);

            // Verificar si playerInputReader está asignado antes de crear PlayerControls
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
        if (!IsOwner) return;

        // Search for components
        movementComponent = GetComponentInChildren<MovementComponent>();
        if (movementComponent == null) {
            logger.Log($"MovementComponent not found for player {OwnerClientId}", this, Logging.LogType.Error);
            return;
        }

        dashComponent = GetComponentInChildren<DashComponent>();
        if (dashComponent == null) {
            logger.Log($"DashComponent not found for player {OwnerClientId}", this, Logging.LogType.Error);
        }

        attackComponent = GetComponentInChildren<AttackComponent>();
        if (attackComponent == null) {
            logger.Log($"AttackComponent not found for player {OwnerClientId}", this, Logging.LogType.Error);
        }

        // Subscribe to PlayerInputReader events instead of creating new PlayerControls
        playerInputReader.MoveEvent += OnMoveInput;
        playerInputReader.DashEvent += OnDashInput;
        playerInputReader.AttackEvent += OnAttackInput;

        logger.Log($"Input configured using PlayerInputReader for player {OwnerClientId}", this);
    }

    private void InitializeInput() {
        if (!IsOwner) return;

        // Search for components in children as well
        movementComponent = GetComponentInChildren<MovementComponent>();
        if (movementComponent == null) {
            logger.Log($"MovementComponent not found for player {OwnerClientId}", this, Logging.LogType.Error);
            return;
        }

        dashComponent = GetComponentInChildren<DashComponent>();
        if (dashComponent == null) {
            logger.Log($"DashComponent not found for player {OwnerClientId}", this, Logging.LogType.Error);
        }

        attackComponent = GetComponentInChildren<AttackComponent>();
        if (attackComponent == null) {
            logger.Log($"AttackComponent not found for player {OwnerClientId}", this, Logging.LogType.Error);
        }

        // Create player-specific controls
        playerControls = new PlayerControls();
        playerControls.Player.Enable();

        // Configure input callbacks - no lambdas to avoid unsubscription issues
        playerControls.Player.Move.performed += OnMovePerformed;
        playerControls.Player.Move.canceled += OnMoveCanceled;
        playerControls.Player.Dash.performed += OnDashPerformed;
        playerControls.Player.Attack.performed += OnAttackPerformed;

        logger.Log($"Input configured for player {OwnerClientId}", this);
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

    public override void OnNetworkDespawn() {
        CleanupInput();
    }

    public override void OnDestroy() {
        // Cleanup in case OnNetworkDespawn wasn't called
        CleanupInput();
        base.OnDestroy();
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
    /// ServerRpc llamado por el cliente para solicitar que el servidor despawnee un loot recogido
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestDespawnLootServerRpc(ulong lootNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        logger?.Log($"[NetworkPlayerController] Cliente {senderId} solicita despawn de loot NetworkObjectId={lootNetworkObjectId}", this);
        
        // Buscar el NetworkObject por ID
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(lootNetworkObjectId, out NetworkObject lootNetworkObject))
        {
            if (lootNetworkObject != null && lootNetworkObject.IsSpawned)
            {
                logger?.Log($"[NetworkPlayerController] Despawneando loot NetworkObjectId={lootNetworkObjectId}", this);
                lootNetworkObject.Despawn(true);
            }
            else
            {
                logger?.Log($"[NetworkPlayerController] NetworkObject {lootNetworkObjectId} ya no está spawneado", this);
            }
        }
        else
        {
            logger?.Log($"[NetworkPlayerController] NetworkObject {lootNetworkObjectId} no encontrado en SpawnManager", this);
        }
    }
}
