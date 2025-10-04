using UnityEngine;

public class PlayerController : MonoBehaviour {
    [SerializeField] public InputReader inputReader;
    [SerializeField] private MovementComponent movementComponent;

    private void OnEnable() {
        inputReader.MoveEvent += movementComponent.OnMove;
        // inputReader.DashEvent += OnDash;
    }

    private void OnDisable() {
        inputReader.MoveEvent -= movementComponent.OnMove;
        // inputReader.DashEvent -= OnDash;
    }
}
