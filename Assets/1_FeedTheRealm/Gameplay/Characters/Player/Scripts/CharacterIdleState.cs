using Game.Core.StateMachine;
using Game.Core.Utils;
using UnityEngine;

/// <summary>
/// State for when the character is idle (not moving).
/// </summary>
public class CharacterIdleState : IMovementState
{
    private IStateMachine stateMachine;

    private MovementController movementController;
    private CharacterAnimator animator;

    public CharacterIdleState(
        IStateMachine sm,
        MovementController movementController,
        CharacterAnimator animator
    )
    {
        this.movementController = movementController;
        this.animator = animator;
        this.stateMachine = sm;
    }

    public void Enter()
    {
        animator.SetMoving(false);
        animator.SetDashing(false);
        movementController.SetDirection(Vector2.zero);
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
        movementController = null;
        animator = null;
    }
}
