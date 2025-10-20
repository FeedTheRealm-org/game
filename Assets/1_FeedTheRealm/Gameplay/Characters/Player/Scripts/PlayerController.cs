using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Connects player input to the movement component.
/// </summary>
public class PlayerController : NetworkBehaviour {
    [SerializeField] public PlayerInputReader inputReader;
    [SerializeField] private GameObject playerPrefab;

    private MovementComponent movementComponent;

    private void Awake() {
        if (playerPrefab != null) {
            movementComponent = playerPrefab.GetComponentInChildren<MovementComponent>();
            if (movementComponent == null) {
                Debug.LogError("MovementComponent not found on the instantiated player prefab.");
            }
        } else {
            Debug.LogError("Player prefab is not assigned in the inspector.");
        }
    }

    private void OnEnable() {
        if (inputReader != null && movementComponent != null) {
            inputReader.MoveEvent += movementComponent.OnMove;
            inputReader.DashEvent += movementComponent.OnDash;

        }
    }

    private void OnDisable() {
        if (inputReader != null && movementComponent != null) {
            inputReader.MoveEvent -= movementComponent.OnMove;
            inputReader.DashEvent -= movementComponent.OnDash;

        }
    }
}
