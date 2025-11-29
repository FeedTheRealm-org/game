using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Synchronizes attack actions for networked characters.
/// This component wraps AttackComponent to handle network synchronization,
/// keeping AttackComponent as a pure MonoBehaviour.
///
/// When a player attacks:
/// - Client: Sends attack request to server via ServerRpc
/// - Server: Validates and executes attack, applies damage to targets
/// - Clients: Receive attack animation/effects via ClientRpc (optional)
/// </summary>
public class NetworkAttackSynchronizer : NetworkBehaviour {
    [SerializeField] private AttackComponent attackComponent;
    [SerializeField] private Logging.Logger logger;

    [Header("Debug")]
    [SerializeField] private bool enableLogs = true;

    [Header("Attack Settings")]
    [SerializeField] private LayerMask targetLayers; // Layers that can be hit (e.g., Enemy)
    [SerializeField] private float hitRadius = 1f;
    [SerializeField] private int attackDamage = 40;

    private Transform hitPoint;
    private bool isLocalPlayerOwned = false;

    // Cache animator reference to avoid GetComponentInChildren calls
    private Animator cachedAnimator;

    private void Awake() {
        if (attackComponent == null) {
            attackComponent = GetComponent<AttackComponent>();
        }

        // Auto-configure from AttackComponent if available
        if (attackComponent != null) {
            hitPoint = attackComponent.GetHitPoint();
            hitRadius = attackComponent.GetHitRadius();
            attackDamage = attackComponent.GetAttackDamage();
            targetLayers = attackComponent.GetTargetLayer();
        }

        // Cache animator reference to avoid expensive GetComponentInChildren calls
        cachedAnimator = GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn() {
        // Check if this is the local player
        isLocalPlayerOwned = IsOwner;

        if (isLocalPlayerOwned) {
            // Local player: Subscribe to local attack component
            // We'll intercept the attack and send it to server
            if (enableLogs)
                logger?.Log($"[NetworkAttackSynchronizer] Local player {OwnerClientId} initialized (Damage: {attackDamage}, Radius: {hitRadius})", this);
        } else {
            if (enableLogs)
                logger?.Log($"[NetworkAttackSynchronizer] Remote player {OwnerClientId} initialized", this);
        }
    }

    /// <summary>
    /// Called by AnimationEvents when attack animation hits
    /// This replaces the local AttackComponent.DetectAttackHit() in multiplayer
    /// </summary>
    public void DetectAttackHit() {
        if (!IsOwner) {
            //logger?.Log("[NetworkAttackSynchronizer] Only owner can trigger attacks!", this, Logging.LogType.Warning);
            return;
        }

        if (hitPoint == null) {
            if (enableLogs)
                logger?.Log("[NetworkAttackSynchronizer] HitPoint not configured!", this, Logging.LogType.Error);
            return;
        }

        // Send attack to server
        DetectAttackHitServerRpc(hitPoint.position);
    }

    /// <summary>
    /// Server receives attack request from client
    /// </summary>
    [ServerRpc]
    private void DetectAttackHitServerRpc(Vector3 attackPosition) {
        if (!IsServer) return;

        if (enableLogs)
            logger?.Log($"[NetworkAttackSynchronizer] Server processing attack at {attackPosition}", this);

        // Broadcast attack animation to all clients (except owner who already played it locally)
        PlayAttackAnimationClientRpc();

        // Detect all colliders in hit radius
        Collider[] hitColliders = Physics.OverlapSphere(attackPosition, hitRadius, targetLayers);

        int hitCount = 0;
        foreach (Collider hit in hitColliders) {
            // Check if target has HealthComponent
            HealthComponent targetHealth = hit.GetComponent<HealthComponent>();
            if (targetHealth != null) {
                // Apply damage on server
                targetHealth.TakeDamage(attackDamage);
                hitCount++;

                if (enableLogs)
                    logger?.Log($"[NetworkAttackSynchronizer] Server applied {attackDamage} damage to {hit.gameObject.name}", this);
            }
        }

        if (hitCount == 0 && enableLogs) {
            logger?.Log($"[NetworkAttackSynchronizer] Server detected no targets hit", this);
        }
    }

    /// <summary>
    /// Broadcast attack animation to all clients
    /// </summary>
    [ClientRpc]
    private void PlayAttackAnimationClientRpc() {
        // Skip if this is the owner (they already played the animation locally via AttackComponent)
        if (IsOwner) return;

        // Play attack animation on remote clients using cached animator
        if (cachedAnimator != null) {
            cachedAnimator.SetTrigger("2_Attack");
            if (enableLogs)
                logger?.Log($"[NetworkAttackSynchronizer] Remote client playing attack animation for player {OwnerClientId}", this);
        }
    }

    /// <summary>
    /// Public method to set hit point and attack parameters from AttackComponent
    /// Call this from AttackComponent.Awake() or in Inspector
    /// </summary>
    public void ConfigureFromAttackComponent(Transform hitPointTransform, float radius, int damage, LayerMask layers) {
        hitPoint = hitPointTransform;
        hitRadius = radius;
        attackDamage = damage;
        targetLayers = layers;

        if (enableLogs)
            logger?.Log($"[NetworkAttackSynchronizer] Configured: Radius={radius}, Damage={damage}, Layers={layers.value}", this);
    }

    private void OnDrawGizmosSelected() {
        if (hitPoint != null) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(hitPoint.position, hitRadius);
        }
    }
}
