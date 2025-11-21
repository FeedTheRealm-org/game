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
    }

    private void OnEnable() {
        if (inputReader != null && movementComponent != null && dashComponent != null) {
            inputReader.MoveEvent += movementComponent.OnMove;
            inputReader.DashEvent += dashComponent.OnDash;
            inputReader.AttackEvent += OnAttackInput;

            logger.Log($"PlayerController subscribed to events. InputReader: {inputReader.name}", this);
        } else {
            logger.Log($"PlayerController OnEnable - Missing components! InputReader: {inputReader != null}, Movement: {movementComponent != null}, Dash: {dashComponent != null}", this, Logging.LogType.Error);
        }
    }

    private void OnDisable() {
        logger.Log("Disabling player controller", this);
        if (inputReader != null && movementComponent != null && dashComponent != null) {
            inputReader.MoveEvent -= movementComponent.OnMove;
            inputReader.DashEvent -= dashComponent.OnDash;
            inputReader.AttackEvent -= OnAttackInput;

            movementComponent = null;
            dashComponent = null;
            attackComponent = null;
            logger.Log("PlayerController unsubscribed from events and cleared references.", this);
        }
    }

    private void OnAttackInput() {
        if (Cursor.visible) {
            return;
        }

        if (attackComponent != null) {
            attackComponent.OnAttack();
            logger.Log("Attack executed", this);
        }
    }
}
