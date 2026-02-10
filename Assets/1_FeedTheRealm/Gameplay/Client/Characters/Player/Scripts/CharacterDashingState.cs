using Game.Core.Client.StateMachine;
using Game.Core.Client.Utils;
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

    public CharacterDashingState(
        IStateMachine sm,
        DashComponent dashComponent,
        CharacterAnimator animator
    )
    {
        this.dashComponent = dashComponent;
        this.animator = animator;
        this.stateMachine = sm;
    }

    public void Enter()
    {
        animator.SetMoving(false);
        animator.SetDashing(true);
        dashComponent.OnDashFinished += OnDashFinished;
        dashComponent.OnDash();
    }

    public void Exit()
    {
        animator.SetDashing(false);
        dashComponent.OnDashFinished -= OnDashFinished;
    }

    public void SetDirection(Vector2 direction)
    {
        lastDirection = direction;
    }

    private void OnDashFinished()
    {
        IMovementState nextState;
        if (VectorTransformations.IsMovementMagnitude(lastDirection))
        {
            nextState = stateMachine.GetMovementStateByType(typeof(CharacterMovingState));
            stateMachine.SetMovementState(nextState);
            stateMachine.CurrentMovementState.SetDirection(lastDirection);
            return;
        }
        nextState = stateMachine.GetMovementStateByType(typeof(CharacterIdleState));
        stateMachine.SetMovementState(nextState);
        return;
    }

    public void Dispose()
    {
        stateMachine = null;
        dashComponent = null;
        animator = null;
    }
}
