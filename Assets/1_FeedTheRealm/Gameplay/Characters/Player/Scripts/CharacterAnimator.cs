using UnityEngine;
using System.Collections.Generic;

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

    private Dictionary<FacingDirection, GameObject> spriteMap;

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
    /// Sets the facing direction of the character.
    /// </summary>
    public void SetFacing(FacingDirection facing) {
        foreach (var kvp in spriteMap) {
            kvp.Value.SetActive(kvp.Key == facing);
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

        float forwardDot = Vector3.Dot(Vector3.forward, moveDir);
        float rightDot = Vector3.Dot(Vector3.right, moveDir);

        if (forwardDot > 0.1f) {
            logger.Log("Facing Back", this);
            SetFacing(FacingDirection.Back);
        } else if (forwardDot < -0.1f) {
            logger.Log("Facing Front", this);
            SetFacing(FacingDirection.Front);
        }
        if (rightDot > 0.1f) {
            logger.Log("Facing Right", this);
            SetFacing(FacingDirection.Right);
        } else if (rightDot < -0.1f) {
            logger.Log("Facing Left", this);
            SetFacing(FacingDirection.Left);
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
