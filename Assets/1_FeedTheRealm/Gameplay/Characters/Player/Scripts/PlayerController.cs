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
            inputReader.AttackEvent += attackComponent.OnAttack;
            inputReader.CursorToggleEvent += cursorToggle;
        }
    }

    private void OnDisable() {
        if (inputReader != null && movementComponent != null && dashComponent != null) {
            inputReader.MoveEvent -= movementComponent.OnMove;
            inputReader.DashEvent -= dashComponent.OnDash;
            inputReader.AttackEvent -= attackComponent.OnAttack;
            inputReader.CursorToggleEvent -= cursorToggle;
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