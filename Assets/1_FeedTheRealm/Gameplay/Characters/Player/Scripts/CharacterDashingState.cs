using Game.Core.StateMachine;
using UnityEngine;

/// <summary>
/// State for when the character is dashing.
/// </summary>
public class CharacterDashingState : IMovementState
{
    private DashComponent dashComponent;
    private CharacterAnimator animator;
    private bool dashTriggered;

    public CharacterDashingState(DashComponent dashComponent, CharacterAnimator animator)
    {
        this.dashComponent = dashComponent;
        this.animator = animator;
    }

    public void Enter(IStateMachine stateMachine)
    {
        animator.SetMoving(false);
        animator.SetDashing(true);
        dashComponent.OnDash();
    }

    public void Exit(IStateMachine stateMachine)
    {
        animator.SetDashing(false);
    }

    public void SetDirection(Vector2 direction) { }
}
