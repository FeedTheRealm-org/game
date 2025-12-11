using UnityEngine;

/// <summary>
/// Handles character animation logic, centralizing animator control.
/// </summary>
public class CharacterAnimator : MonoBehaviour {
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private Logging.Logger logger;

    private void Awake() {
        if (animator == null) {
            animator = GetComponentInChildren<Animator>();
        }
    }

    /// <summary>
    /// Sets the moving animation state.
    /// </summary>
    public void SetMoving(bool isMoving) {
        animator.SetBool("IsRunning", isMoving);
    }

    /// <summary>
    /// Sets the dashing animation state.
    /// </summary>
    public void SetDashing(bool isDashing) {
        // animator.SetBool("IsDashing", isDashing);
    }

    /// <summary>
    /// Triggers the attack animation.
    /// </summary>
    public void PlayAttack() {
        animator.SetTrigger("Slash1H");
    }

    /// <summary>
    /// Triggers the damaged animation.
    /// </summary>
    public void PlayDamaged() {
        animator.SetTrigger("Hit");
    }

    /// <summary>
    /// Triggers the death animation.
    /// </summary>
    public void PlayDeath() {
        animator.SetTrigger("Death");
    }
}
