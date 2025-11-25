using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Synchronizes movement and position for networked characters using the base MovementComponent.
/// This component handles network synchronization while keeping MovementComponent as a pure MonoBehaviour.
/// </summary>
public class NetworkMovementSynchronizer : NetworkBehaviour
{
    [SerializeField] private Logging.Logger logger;
    [SerializeField] private MovementComponent movementComponent;
    [SerializeField] private Rigidbody rb;

    [Header("Network Settings")]
    [SerializeField] private float networkSendRate = 10f; // Updates per second
    [SerializeField] private float positionThreshold = 0.1f; // Minimum change to send
    [SerializeField] private float rotationThreshold = 5f; // Degrees minimum to send

    // Network state
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();

    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();
    private NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>();
    private float lastNetworkSendTime;
    private Vector3 lastSentPosition;
    private Quaternion lastSentRotation;

    private void Awake()
    {
        if (movementComponent == null) movementComponent = GetComponent<MovementComponent>();
        if (rb == null) rb = GetComponent<Rigidbody>();

        // Configure NetworkVariables
        networkPosition.OnValueChanged += OnPositionChanged;
        networkRotation.OnValueChanged += OnRotationChanged;
        networkVelocity.OnValueChanged += OnVelocityChanged;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Owner controls and synchronizes
            logger.Log($"NetworkMovementSynchronizer - Owner: {OwnerClientId}", this);
        }
        else
        {
            // Remote clients - disable local movement and physics
            if (movementComponent != null)
            {
                movementComponent.enabled = false;
            }
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            SyncWithServer();
        }
        else
        {
            // Smooth interpolation for remote players
            float lerpFactor = Mathf.Clamp01(Time.deltaTime * 15f); // Adjust smoothness
            transform.position = Vector3.Lerp(transform.position, networkPosition.Value, lerpFactor);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation.Value, lerpFactor);
        }
    }

    private void SyncWithServer()
    {
        if (!IsServer && IsOwner)
        {
            // Owner client - send state to server periodically
            if (Time.time - lastNetworkSendTime >= 1f / networkSendRate)
            {
                if (ShouldSendTransform())
                {
                    SendTransformToServerRpc(transform.position, transform.rotation, rb != null ? rb.linearVelocity : Vector3.zero);
                    lastNetworkSendTime = Time.time;
                    lastSentPosition = transform.position;
                    lastSentRotation = transform.rotation;
                }
            }
        }
        else if (IsServer && IsOwner)
        {
            // Host - update NetworkVariables directly
            networkPosition.Value = transform.position;
            networkRotation.Value = transform.rotation;
            networkVelocity.Value = rb != null ? rb.linearVelocity : Vector3.zero;
        }
    }

    private bool ShouldSendTransform()
    {
        float positionDiff = Vector3.Distance(transform.position, lastSentPosition);
        float rotationDiff = Quaternion.Angle(transform.rotation, lastSentRotation);

        return positionDiff > positionThreshold || rotationDiff > rotationThreshold;
    }

    [ServerRpc]
    private void SendTransformToServerRpc(Vector3 position, Quaternion rotation, Vector3 velocity)
    {
        // Only server updates NetworkVariables
        networkPosition.Value = position;
        networkRotation.Value = rotation;
        networkVelocity.Value = velocity;
    }

    private void OnPositionChanged(Vector3 oldValue, Vector3 newValue)
    {
        // Interpolation is handled in FixedUpdate for remote clients
    }

    private void OnRotationChanged(Quaternion oldValue, Quaternion newValue)
    {
        // Interpolation is handled in FixedUpdate for remote clients
    }

    private void OnVelocityChanged(Vector3 oldValue, Vector3 newValue)
    {
        if (!IsOwner && rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = newValue;
        }
    }

    /// <summary>
    /// Teleports the character to a new position, synchronizing over the network.
    /// </summary>
    public void Teleport(Vector3 position)
    {
        transform.position = position;
        
        // Reset velocity
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Server has authority to teleport any player
        if (IsServer)
        {
            networkPosition.Value = position;
            networkVelocity.Value = Vector3.zero;
            
            // Force sync to owner client via ClientRpc
            if (!IsOwner)
            {
                TeleportClientRpc(position);
            }
        }
        // Owner client can teleport themselves
        else if (IsOwner)
        {
            SendTransformToServerRpc(position, transform.rotation, Vector3.zero);
        }
    }
    
    [ClientRpc]
    private void TeleportClientRpc(Vector3 position)
    {
        // Only apply if this is the owner client (not server, not other clients)
        if (IsOwner && !IsServer)
        {
            transform.position = position;
            
            // Reset velocity on client
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            logger.Log($"[NetworkMovementSynchronizer] Client {OwnerClientId} teleported to {position} via ClientRpc", this);
        }
    }
}