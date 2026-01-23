using Mirror;
using UnityEngine;

/// <summary>
/// Synchronizes position and rotation for networked enemies.
/// Designed for static/physics-based enemies that don't have active movement AI.
/// This component handles network synchronization for enemies that can be pushed or moved by physics.
///
/// Mirror implementation:
/// - Uses SyncVars for position, rotation, and velocity
/// - Server has authority over all enemy state
/// - Clients interpolate smoothly to server position
/// - NetworkTransformReliable can be used as alternative for simpler cases
/// </summary>
public class NetworkEnemySynchronizer : NetworkBehaviour
{
    [SerializeField]
    private Rigidbody rb;

    [SerializeField]
    private Logging.Logger logger;

    [Header("Network Settings")]
    [SerializeField]
    private float networkSendRate = 10f; // Updates per second

    [SerializeField]
    private float positionThreshold = 0.1f; // Minimum change to send

    [SerializeField]
    private float rotationThreshold = 5f; // Degrees minimum to send

    [Header("Client Interpolation")]
    [SerializeField]
    private float interpolationSpeed = 15f; // How fast clients interpolate to target position

    // SyncVars for network state (server → clients)
    [SyncVar(hook = nameof(OnPositionChanged))]
    private Vector3 networkPosition;

    [SyncVar(hook = nameof(OnRotationChanged))]
    private Quaternion networkRotation;

    [SyncVar]
    private Vector3 networkVelocity;

    // Server-side tracking
    private float lastNetworkSendTime;
    private Vector3 lastSentPosition;
    private Quaternion lastSentRotation;

    // Client interpolation targets
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    public override void OnStartServer()
    {
        // Server: Initialize SyncVars with current transform
        networkPosition = transform.position;
        networkRotation = transform.rotation;
        networkVelocity = rb != null ? rb.linearVelocity : Vector3.zero;

        lastSentPosition = transform.position;
        lastSentRotation = transform.rotation;

        logger?.Log(
            $"[NetworkEnemySynchronizer] Server spawned enemy at {transform.position}",
            this
        );
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            // Clients: Disable physics (server has authority)
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            // Apply initial state immediately
            transform.position = networkPosition;
            transform.rotation = networkRotation;
            targetPosition = networkPosition;
            targetRotation = networkRotation;

            logger?.Log(
                $"[NetworkEnemySynchronizer] Client received enemy at {networkPosition}",
                this
            );
        }
    }

    private void FixedUpdate()
    {
        if (isServer)
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
        if (!isServer)
            return;

        // Check if enough time has passed
        if (Time.time - lastNetworkSendTime < 1f / networkSendRate)
        {
            return;
        }

        // Check if position or rotation changed significantly
        if (ShouldSendTransform())
        {
            networkPosition = transform.position;
            networkRotation = transform.rotation;
            networkVelocity = rb != null ? rb.linearVelocity : Vector3.zero;

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
    /// SyncVar hook called on clients when position changes
    /// </summary>
    private void OnPositionChanged(Vector3 oldValue, Vector3 newValue)
    {
        if (isServer)
            return; // Server already has correct value
        targetPosition = newValue;
    }

    /// <summary>
    /// SyncVar hook called on clients when rotation changes
    /// </summary>
    private void OnRotationChanged(Quaternion oldValue, Quaternion newValue)
    {
        if (isServer)
            return; // Server already has correct value
        targetRotation = newValue;
    }

    /// <summary>
    /// Clients: Smoothly interpolate to server's authoritative position
    /// </summary>
    private void InterpolateToServerState()
    {
        if (isServer)
            return;

        float lerpFactor = Mathf.Clamp01(Time.deltaTime * interpolationSpeed);

        transform.position = Vector3.Lerp(transform.position, targetPosition, lerpFactor);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lerpFactor);

        // Optional: Apply velocity for better prediction
        // This is useful if enemies can be pushed by physics
        if (rb != null && !rb.isKinematic && networkVelocity.sqrMagnitude > 0.01f)
        {
            rb.linearVelocity = networkVelocity;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Server: Teleports the enemy to a new position (useful for respawning)
    /// </summary>
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
        {
            logger?.Log(
                "[NetworkEnemySynchronizer] Only server can teleport enemies!",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        transform.position = position;
        transform.rotation = rotation;

        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Force immediate sync via SyncVars
        networkPosition = position;
        networkRotation = rotation;
        networkVelocity = Vector3.zero;

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
        return networkPosition;
    }

    /// <summary>
    /// Gets the current synchronized rotation
    /// </summary>
    public Quaternion GetNetworkRotation()
    {
        return networkRotation;
    }

    #endregion
}
