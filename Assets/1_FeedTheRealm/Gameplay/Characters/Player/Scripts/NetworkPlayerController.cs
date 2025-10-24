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

    [SerializeField] private Logging.Logger logger;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            logger.Log($"NetworkPlayerController initialized for player {OwnerClientId}", this, Logging.LogType.Info);
            InitializeInput();
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

        // Create player-specific controls
        playerControls = new PlayerControls();
        playerControls.Player.Enable();

        // Configure input callbacks - no lambdas to avoid unsubscription issues
        playerControls.Player.Move.performed += OnMovePerformed;
        playerControls.Player.Move.canceled += OnMoveCanceled;
        playerControls.Player.Dash.performed += OnDashPerformed;

        logger.Log($"Input configured for player {OwnerClientId}", this, Logging.LogType.Info);
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

    public override void OnNetworkDespawn()
    {
        if (playerControls != null)
        {
            playerControls.Player.Disable();
            playerControls.Player.Move.performed -= OnMovePerformed;
            playerControls.Player.Move.canceled -= OnMoveCanceled;
            playerControls.Player.Dash.performed -= OnDashPerformed;
            playerControls.Dispose();
        }
    }
}