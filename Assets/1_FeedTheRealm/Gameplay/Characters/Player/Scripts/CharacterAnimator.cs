using UnityEngine;

/// <summary>
/// Handles character animation logic, centralizing animator control.
/// </summary>
public class CharacterAnimator : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    /// <summary>
    /// Sets the moving animation state.
    /// </summary>
    public void SetMoving(bool isMoving)
    {
        // animator.SetBool("IsMoving", isMoving);
    }

    /// <summary>
    /// Sets the dashing animation state.
    /// </summary>
    public void SetDashing(bool isDashing)
    {
        // animator.SetBool("IsDashing", isDashing);
    }

    /// <summary>
    /// Triggers the attack animation.
    /// </summary>
    public void PlayAttack()
    {
        animator.SetTrigger("2_Attack");
    }

    /// <summary>
    /// Triggers the damaged animation.
    /// </summary>
    public void PlayDamaged()
    {
        animator.SetTrigger("3_Damaged");
    }

    /// <summary>
    /// Triggers the death animation.
    /// </summary>
    public void PlayDeath()
    {
        animator.SetTrigger("4_Death");
    }
}