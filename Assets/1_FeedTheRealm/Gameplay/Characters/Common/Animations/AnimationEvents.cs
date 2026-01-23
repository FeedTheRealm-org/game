using Mirror;
using UnityEngine;

public class AnimationEvents : MonoBehaviour
{
    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private AttackComponent attackComponent;

    [SerializeField]
    private HealthComponent healthComponent;

    // Optional: NetworkAttackSynchronizer for multiplayer (can be assigned in Inspector for better performance)
    [SerializeField]
    private NetworkAttackSynchronizer networkAttackSynchronizer;

    private void Awake()
    {
        // Try to find NetworkAttackSynchronizer (search in this object, parent, and children)
        networkAttackSynchronizer = GetComponent<NetworkAttackSynchronizer>();
        if (networkAttackSynchronizer == null)
        {
            networkAttackSynchronizer = GetComponentInParent<NetworkAttackSynchronizer>();
        }
        if (networkAttackSynchronizer == null)
        {
            networkAttackSynchronizer = GetComponentInChildren<NetworkAttackSynchronizer>();
        }

        if (networkAttackSynchronizer != null)
        {
            logger?.Log("[AnimationEvents] NetworkAttackSynchronizer found!", this);
        }
    }

    private void Attack()
    {
        // In multiplayer, use NetworkAttackSynchronizer if available
        if (NetworkServer.active || NetworkClient.active)
        {
            if (networkAttackSynchronizer != null)
            {
                logger.Log(
                    "[AnimationEvents] Using NetworkAttackSynchronizer for networked attack",
                    this
                );
                networkAttackSynchronizer.DetectAttackHit();
                return;
            }
            else
            {
                logger.Log(
                    "[AnimationEvents] NetworkAttackSynchronizer is NULL! Falling back to local attack (THIS IS WRONG IN MULTIPLAYER!)",
                    this,
                    Logging.LogType.Warning
                );
            }
        }

        // Fallback to local AttackComponent for singleplayer
        if (attackComponent == null)
        {
            logger.Log("AttackComponent reference is missing!", this, Logging.LogType.Error);
            return;
        }

        logger.Log("[AnimationEvents] Using local AttackComponent (singleplayer mode)", this);
        attackComponent.DetectAttackHit();
    }

    private void Die()
    {
        if (healthComponent == null)
        {
            logger.Log("HealthComponent reference is missing!", this);
            return;
        }
        healthComponent.Die();
    }
}
