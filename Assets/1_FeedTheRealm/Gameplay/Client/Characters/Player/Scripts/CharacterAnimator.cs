using System;
using System.Collections.Generic;
using FTR.Core.Client.Enums;
using UnityEngine;

/// <summary>
/// Handles character animation logic, centralizing animator control.
/// </summary>
public class CharacterAnimator : MonoBehaviour
{
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

    [SerializeField]
    private Logging.Logger logger;

    private Dictionary<FacingDirection, GameObject> spriteMap;

    public event Action OnUseAnimationEnd;

    private void Start()
    {
        if (animator == null)
            throw new FTR.Core.Client.Exceptions.MissingFieldException(
                nameof(animator),
                nameof(CharacterAnimator)
            );

        spriteMap = new Dictionary<FacingDirection, GameObject>()
        {
            { FacingDirection.Front, front },
            { FacingDirection.Back, back },
            { FacingDirection.Right, right },
            { FacingDirection.Left, left },
        };

        SetFacing(FacingDirection.Front);
    }

    /* --- Getters --- */

    public bool IsMoving()
    {
        return animator.GetBool("IsRunning");
    }

    /* --- Setters --- */

    public void SetFacing(FacingDirection facing)
    {
        foreach (var kvp in spriteMap)
        {
            kvp.Value.SetActive(kvp.Key == facing);
        }
    }

    public void SetMoving(bool isMoving)
    {
        animator.SetBool("IsRunning", isMoving);
    }

    public void SetDashing(bool isDashing)
    {
        // animator.SetBool("IsDashing", isDashing);
    }

    public void SetAction(bool isAction)
    {
        animator.SetBool("Action", isAction);
    }

    /* --- Players --- */

    public void PlayAttack()
    {
        animator.SetTrigger("Slash1H");
    }

    public void PlayDamaged()
    {
        animator.SetTrigger("Hit");
    }

    public void PlayDeath()
    {
        animator.SetTrigger("Death");
    }

    /* --- Animator Hooks --- */

    public void PlayUseEndHook()
    {
        SetAction(false);
        OnUseAnimationEnd?.Invoke();
    }
}
