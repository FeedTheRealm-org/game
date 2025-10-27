using UnityEngine;

/// <summary>
/// Handles the ground check logic for a given character.
/// </summary>
public class GroundCheckComponent : MonoBehaviour {
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider col;
    [SerializeField] private float groundCheckDistance = 1f;
    [SerializeField] private float groundCheckSphereRadius = 0.4f;
    [SerializeField] private LayerMask groundLayer;

    private Vector3 groundCheckSphereOrigin;

    public RaycastHit LastHit { get; private set; }
    public bool IsGrounded { get; private set; }

    private void Awake() {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (col == null) col = GetComponent<Collider>();
    }

    private void FixedUpdate() {
        Bounds bounds = col.bounds;
        groundCheckSphereOrigin = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);
        if (Physics.SphereCast(groundCheckSphereOrigin, groundCheckSphereRadius, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer)) {
            IsGrounded = true;
            LastHit = hit;
        } else {
            IsGrounded = false;
        }
    }

    /* --- UTILS METHODS --- */

    private void OnDrawGizmos() {
        if (!Application.isPlaying) return;

        Gizmos.color = IsGrounded ? Color.green : Color.red;

        // Draw the sphere cast visualization
        Vector3 endPoint = groundCheckSphereOrigin + Vector3.down * groundCheckDistance;

        Gizmos.DrawWireSphere(groundCheckSphereOrigin, groundCheckSphereRadius);
        Gizmos.DrawLine(groundCheckSphereOrigin, endPoint);
        Gizmos.DrawWireSphere(endPoint, groundCheckSphereRadius);

        if (IsGrounded)
            Gizmos.DrawSphere(LastHit.point, 0.05f);
    }
}


