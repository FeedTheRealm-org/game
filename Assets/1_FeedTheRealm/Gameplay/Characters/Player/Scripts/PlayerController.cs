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

    private void OnEnable() {
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

        // Register callbacks
        if (inputReader != null) {
            inputReader.DashEvent += OnDashInput;
            inputReader.MoveEvent += OnMoveInput;
            inputReader.AttackEvent += OnAttackInput;

            logger.Log("PlayerController subscribed from events.", this);
        }
    }

    private void OnDisable() {
        if (inputReader != null) {
            inputReader.MoveEvent -= OnMoveInput;
            inputReader.DashEvent -= OnDashInput;
            inputReader.AttackEvent -= OnAttackInput;

            logger.Log("PlayerController unsubscribed from events.", this);
        }

        movementComponent = null;
        dashComponent = null;
        attackComponent = null;
    }

    private void OnAttackInput() {
        if (Cursor.visible) {
            return;
        }

        attackComponent?.OnAttack();
    }

    private void OnMoveInput(Vector2 vec) {
        if (Cursor.visible) {
            return;
        }

        movementComponent?.OnMove(vec);
    }

    private void OnDashInput() {
        if (Cursor.visible) {
            return;
        }

        dashComponent?.OnDash();
    }
}
