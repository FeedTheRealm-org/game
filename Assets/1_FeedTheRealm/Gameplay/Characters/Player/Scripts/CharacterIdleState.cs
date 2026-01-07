using Game.Core.StateMachine;
using Game.Core.Utils;
using UnityEngine;

/// <summary>
/// State for when the character is idle (not moving).
/// </summary>
public class CharacterIdleState : IMovementState
{
    private IStateMachine stateMachine;

    private MovementComponent movementComponent;
    private CharacterAnimator animator;

    public CharacterIdleState(MovementComponent movementComponent, CharacterAnimator animator)
    {
        this.movementComponent = movementComponent;
        this.animator = animator;
    }

    public void Enter(IStateMachine stateMachine)
    {
        animator.SetMoving(false);
        animator.SetDashing(false);
        movementComponent.OnMove(Vector2.zero);

        if (this.stateMachine == null)
            this.stateMachine = stateMachine;
    }

    public void Exit(IStateMachine stateMachine)
    {
        if (this.stateMachine != null)
            this.stateMachine = null;
    }

    public void SetDirection(Vector2 direction)
    {
        if (VectorTransformations.IsMovementMagnitude(direction))
        {
            var nextState = stateMachine?.GetMovementStateFromType(typeof(CharacterMovingState));
            stateMachine?.SetMovementState(nextState);
            stateMachine?.CurrentMovementState.SetDirection(direction);
            return;
        }
    }
}
