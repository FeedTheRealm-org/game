using UnityEngine;

public class MovementComponent : MonoBehaviour {
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float moveSpeed = 5f;
    // [SerializeField] private float dashDistance = 2f;

    private Vector3 currentDirection;

    public void OnMove(Vector2 direction) {
        currentDirection = new Vector3(direction.x, 0f, direction.y);
    }

    private void FixedUpdate() {
        Vector3 targetPosition = rb.position + currentDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(targetPosition);
    }
}
