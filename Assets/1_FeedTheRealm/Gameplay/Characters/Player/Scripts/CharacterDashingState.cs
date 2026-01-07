using Game.Core.StateMachine;
using Game.Core.Utils;
using UnityEngine;

/// <summary>
/// State for when the character is dashing.
/// </summary>
public class CharacterDashingState : IMovementState
{
    private IStateMachine stateMachine;

    private DashComponent dashComponent;
    private CharacterAnimator animator;
    private bool dashTriggered;

    private Vector2 lastDirection;

    public CharacterDashingState(DashComponent dashComponent, CharacterAnimator animator)
    {
        this.dashComponent = dashComponent;
        this.animator = animator;
    }

    public void Enter(IStateMachine stateMachine)
    {
        animator.SetMoving(false);
        animator.SetDashing(true);
        dashComponent.OnDashFinished += OnDashFinished;
        dashComponent.OnDash();

        if (this.stateMachine == null)
            this.stateMachine = stateMachine;
    }

    public void Exit(IStateMachine stateMachine)
    {
        animator.SetDashing(false);
        dashComponent.OnDashFinished -= OnDashFinished;

        if (this.stateMachine != null)
            this.stateMachine = null;
    }

    public void SetDirection(Vector2 direction)
    {
        lastDirection = direction;
    }

    private void OnDashFinished()
    {
        if (VectorTransformations.IsMovementMagnitude(lastDirection))
        {
            var nextState = stateMachine?.GetMovementStateFromType(typeof(CharacterMovingState));
            stateMachine?.SetMovementState(nextState);
            stateMachine?.CurrentMovementState.SetDirection(lastDirection);
            return;
        }
        else
        {
            var nextState = stateMachine?.GetMovementStateFromType(typeof(CharacterIdleState));
            stateMachine?.SetMovementState(nextState);
            return;
        }
    }
}
