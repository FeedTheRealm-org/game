using UnityEngine;
using Mirror;

/// <summary>
/// Synchronizes movement and position for networked characters using the base MovementComponent.
/// This component handles network synchronization while keeping MovementComponent as a pure MonoBehaviour.
///
/// Mirror implementation:
/// - Uses NetworkTransformReliable for position/rotation (already on prefab)
/// - SyncVars for velocity and facing direction
/// - Commands for client input to server
/// - Server authority for all state
/// </summary>
public class NetworkMovementSynchronizer : NetworkBehaviour {
    [SerializeField] private Logging.Logger logger;
    [SerializeField] private MovementComponent movementComponent;
    [SerializeField] private Rigidbody rb;

    // SyncVars for state that NetworkTransformReliable doesn't handle
    [SyncVar(hook = nameof(OnVelocityChanged))]
    private Vector3 networkVelocity;

    [SyncVar(hook = nameof(OnFacingChanged))]
    private bool networkFacingRight;

    // Dash support - prevents velocity sync during dash to allow client-side prediction
    private bool isDashing = false;
    private float dashEndTime = 0f;

    private void Awake() {
        if (movementComponent == null) movementComponent = GetComponent<MovementComponent>();
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    public override void OnStartServer() {
        logger?.Log($"[NetworkMovementSynchronizer] Server initialized for netId: {netId}", this);
    }

    public override void OnStartClient() {
        if (!isLocalPlayer) {
            // Remote clients - disable local movement and physics
            if (movementComponent != null) {
                movementComponent.enabled = false;
            }
            if (rb != null) {
                rb.isKinematic = true;
            }
            logger?.Log($"[NetworkMovementSynchronizer] Remote player initialized, local movement disabled", this);
        } else {
            logger?.Log($"[NetworkMovementSynchronizer] Local player initialized", this);
        }
    }

    private void FixedUpdate() {
        // Update dash state
        if (isDashing && Time.time >= dashEndTime) {
            isDashing = false;
        }

        if (!isServer) return;

        // Server updates SyncVars (NetworkTransformReliable handles position/rotation)
        // Skip velocity sync during dash to allow client-side prediction
        if (rb != null && !isDashing) {
            networkVelocity = rb.linearVelocity;
        }

        if (movementComponent != null) {
            networkFacingRight = movementComponent.FacingRight;
        }
    }

    // Hook called on clients when velocity changes
    private void OnVelocityChanged(Vector3 oldValue, Vector3 newValue) {
        if (isServer) return; // Server already has correct value

        // Don't apply server velocity during dash (client has authority during dash)
        if (isDashing) return;

        if (rb != null && !rb.isKinematic) {
            rb.linearVelocity = newValue;
        }
    }

    // Hook called on clients when facing direction changes
    private void OnFacingChanged(bool oldValue, bool newValue) {
        if (isServer) return; // Server already has correct value

        if (movementComponent != null) {
            movementComponent.SetFacing(newValue);
        }
    }

    /// <summary>
    /// Teleports the character to a new position, synchronizing over the network.
    /// Can be called by server or by local player (via Command).
    /// </summary>
    public void Teleport(Vector3 position) {
        if (isServer) {
            // Server has authority - teleport directly
            PerformTeleport(position);
        } else if (isLocalPlayer) {
            // Local player requests teleport from server
            CmdTeleport(position);
        }
    }

    [Command]
    private void CmdTeleport(Vector3 position) {
        // Server validates and performs teleport
        PerformTeleport(position);
    }

    [Server]
    private void PerformTeleport(Vector3 position) {
        transform.position = position;

        // Reset velocity
        if (rb != null && !rb.isKinematic) {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        networkVelocity = Vector3.zero;

        logger?.Log($"[NetworkMovementSynchronizer] Teleported to {position}", this);

        // Sync to all clients via ClientRpc
        RpcTeleport(position);
    }

    [ClientRpc]
    private void RpcTeleport(Vector3 position) {
        if (isServer) return; // Server already set position

        transform.position = position;

        // Reset velocity on clients
        if (rb != null && !rb.isKinematic) {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        logger?.Log($"[NetworkMovementSynchronizer] Client received teleport to {position}", this);
    }

    /// <summary>
    /// Notifies the synchronizer that a dash is starting.
    /// This prevents velocity synchronization from interfering with the dash.
    /// Called by DashComponent when dash begins.
    /// </summary>
    public void NotifyDashStart(float duration) {
        isDashing = true;
        dashEndTime = Time.time + duration;
        //logger?.Log($"[NetworkMovementSynchronizer] Dash started, velocity sync disabled for {duration}s", this);
    }
}
