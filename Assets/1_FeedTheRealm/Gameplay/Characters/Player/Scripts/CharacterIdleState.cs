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

    public CharacterIdleState(
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
        animator.SetMoving(false);
        animator.SetDashing(false);
        movementComponent.OnMove(Vector2.zero);
    }

    public void Exit() { }

    public void SetDirection(Vector2 direction)
    {
        if (VectorTransformations.IsMovementMagnitude(direction))
        {
            var nextState = stateMachine.GetMovementStateByType(typeof(CharacterMovingState));
            stateMachine.SetMovementState(nextState);
        }
    }

    public void Dispose()
    {
        stateMachine = null;
        movementComponent = null;
        animator = null;
    }
}
