using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Synchronizes position and rotation for networked enemies.
/// Designed for static/physics-based enemies that don't have active movement AI.
/// This component handles network synchronization for enemies that can be pushed or moved by physics.
/// </summary>
public class NetworkEnemySynchronizer : NetworkBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Logging.Logger logger;

    [Header("Network Settings")]
    [SerializeField] private float networkSendRate = 10f; // Updates per second
    [SerializeField] private float positionThreshold = 0.1f; // Minimum change to send
    [SerializeField] private float rotationThreshold = 5f; // Degrees minimum to send

    [Header("Client Interpolation")]
    [SerializeField] private float interpolationSpeed = 15f; // How fast clients interpolate to target position

    // Network state
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>(
        writePerm: NetworkVariableWritePermission.Server
    );

    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>(
        writePerm: NetworkVariableWritePermission.Server
    );

    private NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>(
        writePerm: NetworkVariableWritePermission.Server
    );

    // Server-side tracking
    private float lastNetworkSendTime;
    private Vector3 lastSentPosition;
    private Quaternion lastSentRotation;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Server: Initialize network variables with current transform
            networkPosition.Value = transform.position;
            networkRotation.Value = transform.rotation;
            networkVelocity.Value = rb != null ? rb.linearVelocity : Vector3.zero;

            lastSentPosition = transform.position;
            lastSentRotation = transform.rotation;

            logger?.Log($"[NetworkEnemySynchronizer] Server spawned enemy at {transform.position}", this);
        }
        else
        {
            // Clients: Disable physics (server has authority)
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            // Apply initial state immediately
            transform.position = networkPosition.Value;
            transform.rotation = networkRotation.Value;

            logger?.Log($"[NetworkEnemySynchronizer] Client received enemy at {networkPosition.Value}", this);
        }
    }

    private void FixedUpdate()
    {
        if (IsServer)
        {
            // Server: Periodically sync state to clients
            SyncToClients();
        }
        else
        {
            // Clients: Smoothly interpolate to server position
            InterpolateToServerState();
        }
    }

    #region Server Methods

    /// <summary>
    /// Server: Syncs current state to clients if significant change detected
    /// </summary>
    private void SyncToClients()
    {
        if (!IsServer) return;

        // Check if enough time has passed
        if (Time.time - lastNetworkSendTime < 1f / networkSendRate)
        {
            return;
        }

        // Check if position or rotation changed significantly
        if (ShouldSendTransform())
        {
            networkPosition.Value = transform.position;
            networkRotation.Value = transform.rotation;
            networkVelocity.Value = rb != null ? rb.linearVelocity : Vector3.zero;

            lastNetworkSendTime = Time.time;
            lastSentPosition = transform.position;
            lastSentRotation = transform.rotation;

            // logger?.Log($"[NetworkEnemySynchronizer] Server synced position: {transform.position}", this);
        }
    }

    /// <summary>
    /// Checks if transform changed enough to warrant sending update
    /// </summary>
    private bool ShouldSendTransform()
    {
        float positionDiff = Vector3.Distance(transform.position, lastSentPosition);
        float rotationDiff = Quaternion.Angle(transform.rotation, lastSentRotation);

        return positionDiff > positionThreshold || rotationDiff > rotationThreshold;
    }

    #endregion

    #region Client Methods

    /// <summary>
    /// Clients: Smoothly interpolate to server's authoritative position
    /// </summary>
    private void InterpolateToServerState()
    {
        if (IsServer) return;

        float lerpFactor = Mathf.Clamp01(Time.deltaTime * interpolationSpeed);
        
        transform.position = Vector3.Lerp(transform.position, networkPosition.Value, lerpFactor);
        transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation.Value, lerpFactor);

        // Optional: Apply velocity for better prediction
        // This is useful if enemies can be pushed by physics
        if (rb != null && !rb.isKinematic && networkVelocity.Value.sqrMagnitude > 0.01f)
        {
            rb.linearVelocity = networkVelocity.Value;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Server: Teleports the enemy to a new position (useful for respawning)
    /// </summary>
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        if (!IsServer)
        {
            logger?.Log("[NetworkEnemySynchronizer] Only server can teleport enemies!", this, Logging.LogType.Warning);
            return;
        }

        transform.position = position;
        transform.rotation = rotation;

        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Force immediate sync
        networkPosition.Value = position;
        networkRotation.Value = rotation;
        networkVelocity.Value = Vector3.zero;

        lastSentPosition = position;
        lastSentRotation = rotation;
        lastNetworkSendTime = Time.time;

        logger?.Log($"[NetworkEnemySynchronizer] Enemy teleported to {position}", this);
    }

    /// <summary>
    /// Gets the current synchronized position
    /// </summary>
    public Vector3 GetNetworkPosition()
    {
        return networkPosition.Value;
    }

    /// <summary>
    /// Gets the current synchronized rotation
    /// </summary>
    public Quaternion GetNetworkRotation()
    {
        return networkRotation.Value;
    }

    #endregion
}
