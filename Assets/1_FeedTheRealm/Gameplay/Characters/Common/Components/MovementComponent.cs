using UnityEngine;

/// <summary>
/// Handles movement logic for a given character.
/// </summary>
public class MovementComponent : MonoBehaviour {
    [SerializeField]
    private Rigidbody rb;

    [SerializeField]
    private Collider col;

    [SerializeField]
    private GroundCheckComponent groundCheck;

    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private Transform visualRoot;

    private Transform cameraTransform;
    private Vector2 playerDirection;
    private float movingMagnitudeThreash = 0.001f;
    private bool isMoving;
    private bool facingRight = false;

    public Vector3 CurrentDirection { get; private set; }

    private void Awake() {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (col == null) col = GetComponent<Collider>();
        if (groundCheck == null) groundCheck = GetComponent<GroundCheckComponent>();
        cameraTransform = Camera.main.transform; // TODO: add camera manager to handle changes in scenes, etc
    }

    /// <summary>
    /// Called by the input system to set movement direction.
    /// </summary>
    public void OnMove(Vector2 direction) {
        playerDirection = direction;
        isMoving = playerDirection.sqrMagnitude > movingMagnitudeThreash;
        if (isMoving && (facingRight && playerDirection.x < 0f || !facingRight && playerDirection.x > 0f)) {
            flip();
        }
    }

    private void FixedUpdate() {
        updateCurrentDirectionWithCamera();

        // Calculate next position
        Vector3 targetPosition = rb.position;
        targetPosition = rb.position + CurrentDirection * moveSpeed * Time.fixedDeltaTime;

        // Stick to ground logic
        if (groundCheck.IsGrounded && isMoving) {
            Vector3 normal = groundCheck.LastHit.normal;
            rb.linearVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, normal);
            rb.AddForce(-normal * 50f, ForceMode.Acceleration); // Small force to contact ground
        }

        rb.MovePosition(targetPosition);
    }

    /// <summary>
    /// Updates the current movement direction based on camera orientation.
    /// </summary>
    private void updateCurrentDirectionWithCamera() {
        Vector3 camForward = new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z).normalized;
        Vector3 camRight = new Vector3(cameraTransform.right.x, 0f, cameraTransform.right.z).normalized;
        CurrentDirection = (camRight * playerDirection.x + camForward * playerDirection.y).normalized;
    }

    private void flip() {
        facingRight = !facingRight;
        Vector3 localScale = visualRoot.localScale;
        localScale.x *= -1f;
        visualRoot.localScale = localScale;
    }

}
