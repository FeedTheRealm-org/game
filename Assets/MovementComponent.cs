using UnityEngine;
using System.Collections;

public class MovementComponent : MonoBehaviour {
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider col;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dashSpeed = 50f;
    [SerializeField] private float dashDuration = 0.1f;

    private Vector3 currentDirection;
    private bool isGrounded;
    private Vector3 rayOrigin;
    private RaycastHit lastHit;
    private bool isDashing;

    private void Awake() {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (col == null) col = GetComponent<Collider>();
    }

    public void OnMove(Vector2 direction) {
        Debug.Log($"OnMove: {direction}");
        currentDirection = new Vector3(direction.x, 0f, direction.y);
    }

    public void OnDash() {
        if (isDashing) return;

        Vector3 dashDirection = currentDirection.normalized;
        if (dashDirection == Vector3.zero)
            dashDirection = transform.forward;

        StartCoroutine(DashRoutine(dashDirection));
    }

    private IEnumerator DashRoutine(Vector3 direction) {
        isDashing = true;

        // apply instant burst
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(direction * dashSpeed, ForceMode.VelocityChange);

        yield return new WaitForSeconds(dashDuration);

        // stop dash instantly for "snappy" feel
        rb.linearVelocity = Vector3.zero;
        isDashing = false;
    }

    private void FixedUpdate() {
        if (!isDashing) {
            Vector3 targetPosition = rb.position + currentDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);
        }

        if (currentDirection.sqrMagnitude > 0.001f) {
            Quaternion targetRotation = Quaternion.LookRotation(currentDirection, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 0.15f));
        }

        rayOrigin = new Vector3(col.bounds.center.x, col.bounds.min.y, col.bounds.center.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer)) {
            Debug.Log($"Grounded ({hit.collider.name})");
            Debug.DrawRay(rayOrigin, Vector3.down * groundCheckDistance, Color.red);
            isGrounded = true;
            lastHit = hit;
        } else {
            Debug.Log("Not Grounded");
            Debug.DrawRay(rayOrigin, Vector3.down * groundCheckDistance, Color.green);
            isGrounded = false;
        }
    }

    private void OnDrawGizmos() {
        if (!Application.isPlaying) return;

        Gizmos.color = isGrounded ? Color.red : Color.green;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * groundCheckDistance);

        // optional: show a small sphere at hit point
        if (isGrounded)
            Gizmos.DrawSphere(lastHit.point, 0.05f);
    }
}
