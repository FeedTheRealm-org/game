using UnityEngine;
using System.Collections;
using Unity.Netcode;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player movement with network synchronization for MMO.
/// </summary>
public class MovementComponent : NetworkBehaviour {
    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Collider col;
    
    [Header("Movement Settings")]
    [SerializeField] private float groundCheckDistance = 1f;
    [SerializeField] private float groundCheckSphereRadius = 0.4f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dashSpeed = 50f;
    [SerializeField] private float dashDuration = 0.1f;

    [Header("Network Settings")]
    [SerializeField] private float networkSendRate = 10f; // Updates por segundo
    [SerializeField] private float positionThreshold = 0.1f; // Cambio mínimo para enviar
    [SerializeField] private float rotationThreshold = 5f; // Grados mínimo para enviar

    // Estado local
    private Vector3 currentDirection;
    private bool isGrounded;
    private Vector3 groundCheckSphereOrigin;
    private RaycastHit lastHit;
    private bool isDashing;

    // Network state
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();
    private NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>();
    private float lastNetworkSendTime;
    private Vector3 lastSentPosition;
    private Quaternion lastSentRotation;

    private void Awake() {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (col == null) col = GetComponent<Collider>();
        
        // Configurar NetworkVariables
        networkPosition.OnValueChanged += OnPositionChanged;
        networkRotation.OnValueChanged += OnRotationChanged;
        networkVelocity.OnValueChanged += OnVelocityChanged;
    }

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            // El dueño controla y sincroniza
            Debug.Log($"MovementComponent - Owner: {OwnerClientId}");
        } else {
            // Clientes remotos - desactivar física local
            if (rb != null) {
                rb.isKinematic = true;
            }
        }
    }

    #region Input Handling
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

    public void OnDash(InputAction.CallbackContext context) {
        if (!IsOwner) return;
        
        if (context.performed && !isDashing && isGrounded) {
            Vector3 dashDirection = currentDirection.normalized;
            if (dashDirection == Vector3.zero)
                dashDirection = transform.forward;

            StartCoroutine(DashRoutine(dashDirection));
        }
    }
    #endregion

    #region Local Movement
    private void FixedUpdate() {
        if (IsOwner) {
            HandleOwnerMovement();
        } else {
            HandleRemoteMovement();
        }
    }

    private void HandleOwnerMovement() {
        // Aplicar movimiento local
        Vector3 targetPosition = rb.position;
        if (!isDashing) {
            targetPosition = rb.position + currentDirection * moveSpeed * Time.fixedDeltaTime;
        }

        Quaternion targetRotation = rb.rotation;
        if (currentDirection.sqrMagnitude > 0.001f) {
            targetRotation = Quaternion.LookRotation(currentDirection, Vector3.up);
        }

        // Ground check
        Bounds bounds = col.bounds;
        groundCheckSphereOrigin = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);
        if (Physics.SphereCast(groundCheckSphereOrigin, groundCheckSphereRadius, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer)) {
            isGrounded = true;
            lastHit = hit;
            targetPosition.y = hit.point.y + col.bounds.extents.y;
        } else {
            isGrounded = false;
        }

        rb.MovePosition(targetPosition);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 0.15f));

        // Sincronización de red
        SyncWithServer();
    }

    private void HandleRemoteMovement() {
        // Interpolación suave para jugadores remotos
        if (rb != null && !rb.isKinematic) {
            rb.isKinematic = true;
        }
        
        float lerpFactor = Mathf.Clamp01(Time.deltaTime * 15f); // Ajusta la suavidad
        transform.position = Vector3.Lerp(transform.position, networkPosition.Value, lerpFactor);
        transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation.Value, lerpFactor);
    }
    #endregion

    #region Network Synchronization
    private void SyncWithServer() {
        if (!IsServer && IsOwner) {
            // Cliente dueño - enviar estado al servidor periódicamente
            if (Time.time - lastNetworkSendTime >= 1f / networkSendRate) {
                if (ShouldSendTransform()) {
                    SendTransformToServerRpc(transform.position, transform.rotation, rb.linearVelocity);
                    lastNetworkSendTime = Time.time;
                    lastSentPosition = transform.position;
                    lastSentRotation = transform.rotation;
                }
            }
        } else if (IsServer && IsOwner) {
            // Host - actualizar NetworkVariables directamente
            networkPosition.Value = transform.position;
            networkRotation.Value = transform.rotation;
            networkVelocity.Value = rb.linearVelocity;
        }
    }

    private bool ShouldSendTransform() {
        float positionDiff = Vector3.Distance(transform.position, lastSentPosition);
        float rotationDiff = Quaternion.Angle(transform.rotation, lastSentRotation);
        
        return positionDiff > positionThreshold || rotationDiff > rotationThreshold ||
               currentDirection != Vector3.zero || isDashing;
    }

    [ServerRpc]
    private void SendTransformToServerRpc(Vector3 position, Quaternion rotation, Vector3 velocity) {
        // Solo el servidor actualiza los NetworkVariables
        networkPosition.Value = position;
        networkRotation.Value = rotation;
        networkVelocity.Value = velocity;
    }

    private void OnPositionChanged(Vector3 oldValue, Vector3 newValue) {
        // Los clientes reciben actualizaciones aquí
        if (!IsOwner) {
            // La interpolación se maneja en HandleRemoteMovement
        }
    }

    private void OnRotationChanged(Quaternion oldValue, Quaternion newValue) {
        if (!IsOwner) {
            // La interpolación se maneja en HandleRemoteMovement
        }
    }

    private void OnVelocityChanged(Vector3 oldValue, Vector3 newValue) {
        if (!IsOwner && rb != null) {
            rb.linearVelocity = newValue;
        }
    }
    #endregion

    #region Dash
    private IEnumerator DashRoutine(Vector3 direction) {
        isDashing = true;

        // Aplicar dash
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(direction * dashSpeed, ForceMode.VelocityChange);

        // Forzar sincronización inmediata
        if (IsOwner) {
            SendTransformToServerRpc(transform.position, transform.rotation, rb.linearVelocity);
        }

        yield return new WaitForSeconds(dashDuration);

        // Terminar dash
        rb.linearVelocity = Vector3.zero;
        isDashing = false;

        // Sincronizar fin del dash
        if (IsOwner) {
            SendTransformToServerRpc(transform.position, transform.rotation, Vector3.zero);
        }
    }
    #endregion

    #region Public Methods for Game Systems
    public void Teleport(Vector3 position) {
        if (IsOwner) {
            transform.position = position;
            if (IsServer) {
                networkPosition.Value = position;
            } else {
                SendTransformToServerRpc(position, transform.rotation, Vector3.zero);
            }
        }
    }

    public bool IsMoving() {
        return currentDirection != Vector3.zero || isDashing;
    }

    public Vector3 GetCurrentDirection() {
        return currentDirection;
    }
    #endregion

    // Mantener Gizmos y métodos legacy si los necesitas...
    private void OnDrawGizmos() {
        if (!Application.isPlaying) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 endPoint = groundCheckSphereOrigin + Vector3.down * groundCheckDistance;

        Gizmos.DrawWireSphere(groundCheckSphereOrigin, groundCheckSphereRadius);
        Gizmos.DrawLine(groundCheckSphereOrigin, endPoint);
        Gizmos.DrawWireSphere(endPoint, groundCheckSphereRadius);

        if (isGrounded)
            Gizmos.DrawSphere(lastHit.point, 0.05f);
    }
}