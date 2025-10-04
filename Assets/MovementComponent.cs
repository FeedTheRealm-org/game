using UnityEngine;

public class MovementComponent : MonoBehaviour {
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float moveSpeed = 5f;
    // [SerializeField] private float dashDistance = 2f;

    private Vector2 currentDirection;

    public void OnMove(Vector2 direction) {
        currentDirection = direction;
    }

    private void Update() {
        Vector3 movement = new Vector3(currentDirection.x, 0f, currentDirection.y) * moveSpeed * Time.deltaTime;
        playerTransform.position += movement;
    }
}
