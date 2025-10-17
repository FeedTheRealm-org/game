using UnityEngine;

/// <summary>
/// Connects player input to the movement component.
/// </summary>
public class PlayerController : MonoBehaviour {
    [SerializeField] public PlayerInputReader inputReader;
    [SerializeField] private GameObject playerPrefab;

    private MovementComponent movementComponent;
    private DashComponent dashComponent;

    private void Awake() {
        if (playerPrefab != null) {
            movementComponent = playerPrefab.GetComponentInChildren<MovementComponent>();
            if (movementComponent == null) {
                Debug.LogError("MovementComponent not found on the instantiated player prefab.");
            }
        } else {
            Debug.LogError("Player prefab is not assigned in the inspector.");
        }

        if (playerPrefab != null) {
            dashComponent = playerPrefab.GetComponentInChildren<DashComponent>();
            if (dashComponent == null) {
                Debug.LogError("DashComponent not found on the instantiated player prefab.");
            }
        } else {
            Debug.LogError("Player prefab is not assigned in the inspector.");
        }
    }

    private void OnEnable() {
        if (inputReader != null && movementComponent != null) {
            inputReader.MoveEvent += movementComponent.OnMove;
            inputReader.DashEvent += dashComponent.OnDash;

        }
    }

    private void OnDisable() {
        if (inputReader != null && movementComponent != null) {
            inputReader.MoveEvent -= movementComponent.OnMove;
            inputReader.DashEvent -= dashComponent.OnDash;

        }
    }
}
