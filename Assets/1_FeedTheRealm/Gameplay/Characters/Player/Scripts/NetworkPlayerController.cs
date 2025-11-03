using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

/// <summary>
/// Handles networked player input, connecting to MovementComponent and DashComponent.
/// This replaces the need for MP-specific versions by keeping components modular.
/// </summary>
public class NetworkPlayerController : NetworkBehaviour
{
    [Header("Input")]
    private PlayerControls playerControls;
    private MovementComponent movementComponent;
    private DashComponent dashComponent;
    private AttackComponent attackComponent;

    [SerializeField] private Logging.Logger logger;
    [SerializeField] private PlayerInputReader playerInputReader;
    
    private bool isInventoryOpen = false;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            logger.Log($"NetworkPlayerController initialized for player {OwnerClientId}", this);
            InitializeInput();
            SubscribeToUIEvents();
        }
    }

    private void InitializeInput()
    {
        if (!IsOwner) return;

        // Search for components in children as well
        movementComponent = GetComponentInChildren<MovementComponent>();
        if (movementComponent == null)
        {
            logger.Log($"MovementComponent not found for player {OwnerClientId}", this, Logging.LogType.Error);
            return;
        }

        dashComponent = GetComponentInChildren<DashComponent>();
        if (dashComponent == null)
        {
            logger.Log($"DashComponent not found for player {OwnerClientId}", this, Logging.LogType.Error);
        }

        attackComponent = GetComponentInChildren<AttackComponent>();
        if (attackComponent == null)
        {
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

    private void SubscribeToUIEvents()
    {
        if (playerInputReader != null)
        {
            playerInputReader.InventoryOpenedEvent += OnInventoryOpened;
            playerInputReader.InventoryClosedEvent += OnInventoryClosed;
            logger.Log($"NetworkPlayerController subscribed to UI events", this);
        }
        else
        {
            logger.Log($"PlayerInputReader not assigned in NetworkPlayerController", this, Logging.LogType.Warning);
        }
    }

    // Separate methods to avoid lambda issues
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        Vector2 direction = context.ReadValue<Vector2>();
        movementComponent?.OnMove(direction);
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        movementComponent?.OnMove(Vector2.zero);
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        dashComponent?.OnDash();
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {        
        if (isInventoryOpen)
        {
            logger.Log("Attack blocked - Inventory is open", this);
            return;
        }

        attackComponent?.OnAttack();
    }

    private void OnInventoryOpened()
    {
        isInventoryOpen = true;
        logger.Log($"NetworkPlayer {OwnerClientId} - Inventory opened, attacks disabled", this);
    }

    private void OnInventoryClosed()
    {
        isInventoryOpen = false;
        logger.Log($"NetworkPlayer {OwnerClientId} - Inventory closed, attacks enabled", this);
    }

    public override void OnNetworkDespawn()
    {
        CleanupInput();
        UnsubscribeFromUIEvents();
    }
    
    public override void OnDestroy()
    {
        // Cleanup in case OnNetworkDespawn wasn't called
        CleanupInput();
        UnsubscribeFromUIEvents();
        base.OnDestroy();
    }
    
    private void CleanupInput()
    {
        if (playerControls != null)
        {
            playerControls.Player.Disable();
            playerControls.Player.Move.performed -= OnMovePerformed;
            playerControls.Player.Move.canceled -= OnMoveCanceled;
            playerControls.Player.Dash.performed -= OnDashPerformed;
            playerControls.Player.Attack.performed -= OnAttackPerformed;
            playerControls.Dispose();
            playerControls = null;
        }
    }

    private void UnsubscribeFromUIEvents()
    {
        if (playerInputReader != null)
        {
            playerInputReader.InventoryOpenedEvent -= OnInventoryOpened;
            playerInputReader.InventoryClosedEvent -= OnInventoryClosed;
        }
    }
}