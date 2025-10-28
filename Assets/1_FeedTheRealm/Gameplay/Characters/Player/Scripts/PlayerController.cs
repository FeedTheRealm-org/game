using UnityEngine;

/// <summary>
/// Connects player input to the movement component.
/// </summary>
public class PlayerController : MonoBehaviour {
    [SerializeField]
    public PlayerInputReader inputReader;

    [SerializeField]
    private GameObject playerPrefab;

    [SerializeField]
    private Logging.Logger logger;

    private MovementComponent movementComponent;
    private DashComponent dashComponent;
    private AttackComponent attackComponent;
    private InventoryController inventoryController;

    private void Awake() {
        if (playerPrefab == null) {
            logger.Log("Player prefab is not assigned in the inspector.", this, Logging.LogType.Error);
        }

        movementComponent = playerPrefab.GetComponentInChildren<MovementComponent>();
        if (movementComponent == null) {
            logger.Log("MovementComponent not found on the instantiated player prefab.", this, Logging.LogType.Error);
        }

        dashComponent = playerPrefab.GetComponentInChildren<DashComponent>();
        if (dashComponent == null) {
            logger.Log("DashComponent not found on the instantiated player prefab.", this, Logging.LogType.Error);
        }

        attackComponent = playerPrefab.GetComponentInChildren<AttackComponent>();
        if (attackComponent == null) {
            logger.Log("AttackComponent not found on the instantiated player prefab.", this, Logging.LogType.Error);
        }

        // Buscar el InventoryController en la escena
        inventoryController = FindFirstObjectByType<InventoryController>();
        if (inventoryController == null) {
            logger.Log("InventoryController not found in the scene.", this, Logging.LogType.Warning);
        }

        cursorToggle();
    }

    private void OnEnable() {
        if (inputReader != null && movementComponent != null && dashComponent != null) {
            inputReader.MoveEvent += movementComponent.OnMove;
            inputReader.DashEvent += dashComponent.OnDash;
            inputReader.AttackEvent += OnAttackInput;
            inputReader.CursorToggleEvent += cursorToggle;
        }
    }

    private void OnDisable() {
        if (inputReader != null && movementComponent != null && dashComponent != null) {
            inputReader.MoveEvent -= movementComponent.OnMove;
            inputReader.DashEvent -= dashComponent.OnDash;
            inputReader.AttackEvent -= OnAttackInput;
            inputReader.CursorToggleEvent -= cursorToggle;
        }
    }

    private void OnAttackInput() {
        // No permitir ataque si el inventario está abierto
        if (inventoryController != null && inventoryController.IsInventoryOpen()) {
            logger.Log("Attack blocked - Inventory is open", this);
            return;
        }

        if (attackComponent != null) {
            attackComponent.OnAttack();
        }
    }

    private void cursorToggle() {
        if (Cursor.visible) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            logger.Log("Cursor toggled OFF", this);
        } else {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            logger.Log("Cursor toggled ON", this);
        }
    }
}