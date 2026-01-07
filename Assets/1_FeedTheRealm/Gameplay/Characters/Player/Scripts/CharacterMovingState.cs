using Game.Core.StateMachine;
using Game.Core.Utils;
using UnityEngine;

/// <summary>
/// State for when the character is moving.
/// </summary>
public class CharacterMovingState : IMovementState
{
    private IStateMachine stateMachine;

    private MovementComponent movementComponent;
    private CharacterAnimator animator;

    public CharacterMovingState(MovementComponent movementComponent, CharacterAnimator animator)
    {
        this.movementComponent = movementComponent;
        this.animator = animator;
    }

    public void Enter(IStateMachine stateMachine)
    {
        animator.SetMoving(true);
        animator.SetDashing(false);

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
        if (!VectorTransformations.IsMovementMagnitude(direction))
        {
            var nextState = stateMachine?.GetMovementStateFromType(typeof(CharacterIdleState));
            stateMachine?.SetMovementState(nextState);
            return;
        }

        movementComponent.OnMove(direction);
        animator.SetDirection(direction);
    }
}
