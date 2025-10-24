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

    private bool isAttacking = false;
    private Animator _animator;

    private void Start() {
        _animator = GetComponentInChildren<Animator>();
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
}
