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

    private CharacterStateMachine characterStateMachine;

    private void OnEnable() {
        if (playerPrefab == null) {
            logger.Log("Player prefab is not assigned in the inspector.", this, Logging.LogType.Error);
        }

        characterStateMachine = playerPrefab.GetComponentInChildren<CharacterStateMachine>();
        if (characterStateMachine == null) {
            logger.Log("CharacterStateMachine not found on the instantiated player prefab.", this, Logging.LogType.Error);
        }

        // Register callbacks
        if (inputReader != null) {
            inputReader.DashEvent += OnDashInput;
            inputReader.MoveEvent += OnMoveInput;
            inputReader.AttackDownEvent += OnAttackDownInput;
            inputReader.AttackUpEvent += OnAttackUpInput;

            logger.Log("PlayerController subscribed from events.", this);
        }
    }

    private void OnDisable() {
        if (inputReader != null) {
            inputReader.MoveEvent -= OnMoveInput;
            inputReader.DashEvent -= OnDashInput;
            inputReader.AttackDownEvent -= OnAttackDownInput;
            inputReader.AttackUpEvent -= OnAttackUpInput;

            logger.Log("PlayerController unsubscribed from events.", this);
        }

        characterStateMachine = null;
    }

    private void OnAttackDownInput() {
        if (Cursor.visible) {
            return;
        }

        characterStateMachine?.OnAttackDown();
    }

    private void OnAttackUpInput() {
        if (Cursor.visible) {
            return;
        }

        characterStateMachine?.OnAttackUp();
    }

    private void OnMoveInput(Vector2 vec) {
        if (Cursor.visible) {
            return;
        }

        characterStateMachine?.OnMove(vec);
    }

    private void OnDashInput() {
        if (Cursor.visible) {
            return;
        }

        characterStateMachine?.OnDash();
    }
}
