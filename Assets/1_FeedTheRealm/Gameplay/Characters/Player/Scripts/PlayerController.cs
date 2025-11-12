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
    
    private bool isInventoryOpen = false;

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
        cursorToggle();
    }

    private void OnEnable() {
        if (inputReader != null && movementComponent != null && dashComponent != null) {
            inputReader.MoveEvent += movementComponent.OnMove;
            inputReader.DashEvent += dashComponent.OnDash;
            inputReader.AttackEvent += OnAttackInput;
            inputReader.CursorToggleEvent += cursorToggle;
            
            // Suscribirse a eventos de inventario
            inputReader.InventoryOpenedEvent += OnInventoryOpened;
            inputReader.InventoryClosedEvent += OnInventoryClosed;

            logger.Log($"PlayerController subscribed to events. InputReader: {inputReader.name}", this);
        }
        else
        {
            logger.Log($"PlayerController OnEnable - Missing components! InputReader: {inputReader != null}, Movement: {movementComponent != null}, Dash: {dashComponent != null}", this, Logging.LogType.Error);
        }
    }

    private void OnDisable() {
        if (inputReader != null && movementComponent != null && dashComponent != null) {
            inputReader.MoveEvent -= movementComponent.OnMove;
            inputReader.DashEvent -= dashComponent.OnDash;
            inputReader.AttackEvent -= OnAttackInput;
            inputReader.CursorToggleEvent -= cursorToggle;
            
            // Desuscribirse de eventos de inventario
            inputReader.InventoryOpenedEvent -= OnInventoryOpened;
            inputReader.InventoryClosedEvent -= OnInventoryClosed;
        }
    }

    private void OnAttackInput() {
        // No permitir ataque si el inventario está abierto
        if (isInventoryOpen) {
            logger.Log("Attack blocked - Inventory is open", this);
            return;
        }

        if (attackComponent != null) {
            attackComponent.OnAttack();
            logger.Log("Attack executed", this);
        }
    }

    private void OnInventoryOpened() {
        isInventoryOpen = true;
        logger.Log("Inventory opened - Attacks disabled", this);
    }

    private void OnInventoryClosed() {
        isInventoryOpen = false;
        logger.Log("Inventory closed - Attacks enabled", this);
    }

    private void cursorToggle() {
        bool shouldShowCursor = !Cursor.visible;
        
        if (shouldShowCursor) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            logger.Log("Cursor mostrado (toggle)", this);
        } else {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            logger.Log("Cursor oculto (toggle)", this);
        }
    }
}