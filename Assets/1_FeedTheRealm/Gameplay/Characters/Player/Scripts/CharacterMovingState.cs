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

    public CharacterMovingState(
        IStateMachine sm,
        MovementComponent movementComponent,
        CharacterAnimator animator
    )
    {
        this.movementComponent = movementComponent;
        this.animator = animator;
        this.stateMachine = sm;
    }

    public void Enter()
    {
        animator.SetMoving(true);
        animator.SetDashing(false);
    }

    public void Exit() { }

    public void SetDirection(Vector2 direction)
    {
        if (!VectorTransformations.IsMovementMagnitude(direction))
        {
            var nextState = stateMachine.GetMovementStateByType(typeof(CharacterIdleState));
            stateMachine.SetMovementState(nextState);
            return;
        }

        movementComponent.OnMove(direction);
        animator.SetDirection(direction);
    }

    public void Dispose()
    {
        stateMachine = null;
        movementComponent = null;
        animator = null;
    }
}
