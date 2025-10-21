using UnityEngine;
using System.Collections;
using Unity.Netcode;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player movement including walking and dashing.
/// </summary>
public class MovementComponent : NetworkBehaviour {
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider col;
    [SerializeField] private float groundCheckDistance = 1f;
    [SerializeField] private float groundCheckSphereRadius = 0.4f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dashSpeed = 50f;
    [SerializeField] private float dashDuration = 0.1f;

    private Vector3 currentDirection;
    private bool isGrounded;
    private Vector3 groundCheckSphereOrigin;
    private RaycastHit lastHit;
    private bool isDashing;

    private void Awake() {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (col == null) col = GetComponent<Collider>();
    }

    /// <summary>
    /// Called by the input system to set movement direction.
    /// </summary>
    public void OnMove(InputAction.CallbackContext context) {
        if (!IsOwner) return;
        
        if (context.performed) {
            Vector2 direction = context.ReadValue<Vector2>();
            currentDirection = new Vector3(direction.x, 0f, direction.y);
        }
        else if (context.canceled) {
            currentDirection = Vector3.zero;
        }
    }

    /// <summary>
    /// Called by the input system to initiate a dash.
    /// </summary>
    public void OnDash(InputAction.CallbackContext context) {
        if (!IsOwner) return;
        
        if (context.performed && !isDashing && isGrounded) {
            Vector3 dashDirection = currentDirection.normalized;
            if (dashDirection == Vector3.zero)
                dashDirection = transform.forward;

            StartCoroutine(DashRoutine(dashDirection));
        }
    }

    /// <summary>
    /// Coroutine to handle the dash force application for the defined duration.
    /// </summary>
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
        if (!IsOwner) return;

        Vector3 targetPosition = rb.position;
        if (!isDashing) {
            targetPosition = rb.position + currentDirection * moveSpeed * Time.fixedDeltaTime;
        }

        Quaternion targetRotation = rb.rotation;
        if (currentDirection.sqrMagnitude > 0.001f) {
            targetRotation = Quaternion.LookRotation(currentDirection, Vector3.up);
        }

        Bounds bounds = col.bounds;
        groundCheckSphereOrigin = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);
        if (Physics.SphereCast(groundCheckSphereOrigin, groundCheckSphereRadius, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer)) {
            isGrounded = true;
            lastHit = hit;
            // stick to ground
            targetPosition.y = hit.point.y + col.bounds.extents.y;
        } else {
            isGrounded = false;
        }

        rb.MovePosition(targetPosition);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 0.15f));
    }

    private void OnDrawGizmos() {
        if (!Application.isPlaying) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;

        // Draw the sphere cast visualization
        Vector3 endPoint = groundCheckSphereOrigin + Vector3.down * groundCheckDistance;

        Gizmos.DrawWireSphere(groundCheckSphereOrigin, groundCheckSphereRadius);
        Gizmos.DrawLine(groundCheckSphereOrigin, endPoint);
        Gizmos.DrawWireSphere(endPoint, groundCheckSphereRadius);

        if (isGrounded)
            Gizmos.DrawSphere(lastHit.point, 0.05f);
    }

    // Métodos legacy para compatibilidad (puedes eliminarlos después si no los usas)
    public void OnMove(Vector2 direction) {
        if (IsOwner) {
            currentDirection = new Vector3(direction.x, 0f, direction.y);
        }
    }

    public void OnDash() {
        if (IsOwner && !isDashing && isGrounded) {
            Vector3 dashDirection = currentDirection.normalized;
            if (dashDirection == Vector3.zero)
                dashDirection = transform.forward;

            StartCoroutine(DashRoutine(dashDirection));
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Debug.Log($"=== DEBUG Player Structure {OwnerClientId} ===");
            Debug.Log($"Player GameObject: {gameObject.name}");
            Debug.Log($"Transform position: {transform.position}");
            
            // Buscar componentes
            var playerController = GetComponent<PlayerController>();
            Debug.Log($"PlayerController: {playerController != null}");
            
            var movement = GetComponent<MovementComponent>();
            Debug.Log($"MovementComponent en padre: {movement != null}");
            
            if (movement == null)
            {
                movement = GetComponentInChildren<MovementComponent>();
                Debug.Log($"MovementComponent en hijos: {movement != null}");
                
                if (movement != null)
                {
                    Debug.Log($"Encontrado en: {movement.gameObject.name}");
                }
            }
            
            // Listar todos los hijos
            Debug.Log($"Hijos del player:");
            foreach (Transform child in transform)
            {
                Debug.Log($" - {child.name}");
                foreach (Transform grandchild in child)
                {
                    Debug.Log($"   - {grandchild.name}");
                }
            }
        }
    }
}