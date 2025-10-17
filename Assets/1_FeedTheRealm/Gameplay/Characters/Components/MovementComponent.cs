using UnityEngine;

/// <summary>
/// Handles movement logic for a given character.
/// </summary>
public class MovementComponent : MonoBehaviour {
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider col;
    [SerializeField] private GroundCheckComponent groundCheck;

    [SerializeField] private float moveSpeed = 5f;
    // verticalLerpFactor is the interpolation factor for how smooth the stick to ground is
    [SerializeField] private float verticalLerpFactor = 10f;

    private float movingMagnitudeThreash = 0.001f;
    private bool isMoving;

    public Vector3 CurrentDirection { get; private set; }

    private void Awake() {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (col == null) col = GetComponent<Collider>();
        if (groundCheck == null) groundCheck = GetComponent<GroundCheckComponent>();
    }

    /// <summary>
    /// Called by the input system to set movement direction.
    /// </summary>
    public void OnMove(Vector2 direction) {
        CurrentDirection = new Vector3(direction.x, 0f, direction.y);
        isMoving = false;
        if (CurrentDirection.sqrMagnitude > movingMagnitudeThreash) {
            isMoving = true; // if movement is noticeable
        }
    }

    private void FixedUpdate() {
        Vector3 targetPosition = rb.position;
        targetPosition = rb.position + CurrentDirection * moveSpeed * Time.fixedDeltaTime;

        Quaternion targetRotation = rb.rotation;
        if (isMoving) {
            targetRotation = Quaternion.LookRotation(CurrentDirection, Vector3.up);
        }

        if (groundCheck.IsGrounded && isMoving) {
            float targetY = groundCheck.LastHit.point.y + col.bounds.extents.y;
            targetPosition.y = Mathf.Lerp(targetPosition.y, targetY, Time.fixedDeltaTime * verticalLerpFactor);
        }

        rb.MovePosition(targetPosition);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 0.15f));
    }
}
