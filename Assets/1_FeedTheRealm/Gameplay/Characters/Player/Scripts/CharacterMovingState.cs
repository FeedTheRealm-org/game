using Game.Core.StateMachine;
using UnityEngine;

/// <summary>
/// State for when the character is moving.
/// </summary>
public class CharacterMovingState : IMovementState
{
    private MovementComponent movementComponent;
    private CharacterAnimator animator;
    private Vector2 currentDirection;

    public CharacterMovingState(MovementComponent movementComponent, CharacterAnimator animator)
    {
        this.movementComponent = movementComponent;
        this.animator = animator;
    }

    public void Enter(IStateMachine stateMachine)
    {
        animator.SetMoving(true);
        animator.SetDashing(false);
    }

    public void Exit(IStateMachine stateMachine) { }

    /// <summary>
    /// Updates the movement direction.
    /// </summary>
    public void SetDirection(Vector2 direction)
    {
        currentDirection = direction;
        movementComponent.OnMove(direction);
        animator.SetDirection(direction);
    }
}
