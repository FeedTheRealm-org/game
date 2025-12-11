
using UnityEngine;

public class CharacterAttackState : IState
{
    private AttackComponent attackComponent;
    private CharacterAnimator animator;
    private bool attackTriggered;

    public CharacterAttackState(AttackComponent attackComponent, CharacterAnimator animator)
    {
        this.attackComponent = attackComponent;
        this.animator = animator;
    }

    public void Enter()
    {
        attackComponent.OnAttack();
        animator.PlayAttack();
    }

    public void Exit()
    {
        // No specific exit logic
    }
}
