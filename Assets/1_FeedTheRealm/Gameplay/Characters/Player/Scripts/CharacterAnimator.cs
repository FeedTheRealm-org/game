using UnityEngine;

public enum FacingDirection {
    Front,
    Back,
    Right,
    Left
}

/// <summary>
/// Handles character animation logic, centralizing animator control.
/// </summary>
public class CharacterAnimator : MonoBehaviour {
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private GameObject front;

    [SerializeField]
    private GameObject back;

    [SerializeField]
    private GameObject right;

    [SerializeField]
    private GameObject left;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    private void Awake() {
        spriteMap = new Dictionary<FacingDirection, GameObject>() {
        { FacingDirection.Front, front },
        { FacingDirection.Back, back },
        { FacingDirection.Right, right },
        { FacingDirection.Left, left }
    };

        // optional: disable all on start
        SetFacing(FacingDirection.Front);
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

    public void SetAction(bool isAction) {
        animator.SetBool("Action", isAction);
    }

    public void SetDirection(Vector2 direction) {
        Vector3 moveDir = new Vector3(direction.x, 0f, direction.y).normalized;

        float forwardDot = Vector3.Dot(transform.forward, moveDir);
        float rightDot = Vector3.Dot(transform.right, moveDir);

        if (forwardDot > 0.1f) {
            front.SetActive(true);
            back.SetActive(false);
            right.SetActive(false);
            left.SetActive(false);
        } else if (forwardDot < -0.1f) {
            Debug.Log("Backward");
        }
        if (rightDot > 0.1f) {
            Debug.Log("Right");
        } else if (rightDot < -0.1f) {
            Debug.Log("Left");
        }
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
