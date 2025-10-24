using UnityEngine;
using System.Collections;

/// <summary>
/// Handles attack actions for the player character.
/// </summary>
public class AttackComponent : MonoBehaviour {
    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private float attackCooldown = 0.4f;

    [SerializeField]
    private Transform hitPoint;

    [SerializeField]
    private float hitRadius;

    [SerializeField]
    private LayerMask targetLayer;

    private bool isAttacking = false;
    private Animator _animator;

    private void Start() {
        _animator = GetComponentInChildren<Animator>();
    }

    public void DetectAttackHit() {
        Collider[] hitTargets = Physics.OverlapSphere(hitPoint.position, hitRadius, targetLayer);
        foreach (Collider target in hitTargets) {
            logger.Log($"Hit target: {target.name}", this);
            // Here you can add logic to apply damage to the target
        }

        if (hitTargets.Length == 0) {
            logger.Log("No targets hit", this);
        }
    }

    /// <summary>
    /// Triggers the attack animation.
    /// </summary>
    public void OnAttack() {
        if (isAttacking) return;
        logger.Log("Attack event triggered", this);

        _animator.SetTrigger("2_Attack");

        isAttacking = true;
        StartCoroutine(resetAttackCooldown());
    }

    /// <summary>
    /// Resets the attack cooldown after a delay.
    /// </summary>
    private IEnumerator resetAttackCooldown() {
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    private void OnDrawGizmos() {
        if (hitPoint == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(hitPoint.position, hitRadius);
    }

}
