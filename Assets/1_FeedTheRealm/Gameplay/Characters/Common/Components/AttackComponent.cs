using System.Collections;
using UnityEngine;

/// <summary>
/// Handles attack actions for the player character.
/// </summary>
public class AttackComponent : MonoBehaviour
{
    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private float attackCooldown = 0.4f;

    [SerializeField]
    private int attackDamage = 40;

    [SerializeField]
    private Transform hitPoint;

    [SerializeField]
    private float hitRadius;

    [SerializeField]
    private LayerMask targetLayer;

    private bool isAttacking = false;
    private Animator _animator;

    public System.Action OnAttackFinished;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    public void DetectAttackHit()
    {
        Collider[] hitTargets = Physics.OverlapSphere(hitPoint.position, hitRadius, targetLayer);
        foreach (Collider target in hitTargets)
        {
            logger.Log($"Hit target: {target.name}", this);
            target.GetComponent<HealthComponent>()?.TakeDamage(attackDamage);
        }

        if (hitTargets.Length == 0)
        {
            logger.Log("No targets hit", this);
        }
    }

    /// <summary>
    /// Triggers the attack animation.
    /// </summary>
    public void OnAttack()
    {
        if (isAttacking)
            return;
        logger.Log("Attack event triggered", this);

        isAttacking = true;
        StartCoroutine(resetAttackCooldown());
    }

    /// <summary>
    /// Resets the attack cooldown after a delay.
    /// </summary>
    private IEnumerator resetAttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
        OnAttackFinished?.Invoke();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (hitPoint == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(hitPoint.position, hitRadius);
    }
#endif

    #region Public Getters for NetworkAttackSynchronizer

    /// <summary>
    /// Gets the attack hit point transform (for network synchronization)
    /// </summary>
    public Transform GetHitPoint() => hitPoint;

    /// <summary>
    /// Gets the attack hit radius (for network synchronization)
    /// </summary>
    public float GetHitRadius() => hitRadius;

    /// <summary>
    /// Gets the attack damage amount (for network synchronization)
    /// </summary>
    public int GetAttackDamage() => attackDamage;

    /// <summary>
    /// Gets the target layer mask (for network synchronization)
    /// </summary>
    public LayerMask GetTargetLayer() => targetLayer;

    #endregion
}
