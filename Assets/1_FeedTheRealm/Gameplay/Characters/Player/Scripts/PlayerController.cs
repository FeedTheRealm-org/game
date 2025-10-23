using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour 
{
    [Header("Input")]
    private PlayerControls playerControls;
    private MovementComponent movementComponent;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Debug.Log($"PlayerController inicializado para jugador {OwnerClientId}");
            InitializeInput();
        }
    }

    private void InitializeInput()
    {
        if (!IsOwner) return;

        // Search for MovementComponent in children as well
        movementComponent = GetComponentInChildren<MovementComponent>();
        if (movementComponent == null)
        {
            Debug.LogError($"MovementComponent no encontrado para jugador {OwnerClientId}");
            Debug.Log($"Buscando en: {gameObject.name}, hijos: {transform.childCount}");
            return;
        }

        // Crear controles específicos para este jugador
        playerControls = new PlayerControls();
        playerControls.Player.Enable();

        // Configurar callbacks de input - SIN lambda para evitar problemas de desuscripción
        playerControls.Player.Move.performed += OnMovePerformed;
        playerControls.Player.Move.canceled += OnMoveCanceled;
        playerControls.Player.Dash.performed += OnDashPerformed;

        Debug.Log($"Input configurado para jugador {OwnerClientId}");
    }

    // Métodos separados para evitar problemas con lambdas
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        movementComponent?.OnMove(context);
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        movementComponent?.OnMove(context);
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        movementComponent?.OnDash(context);
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