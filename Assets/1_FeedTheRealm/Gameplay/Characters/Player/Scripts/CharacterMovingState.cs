using UnityEngine;

/// <summary>
/// State for when the character is moving.
/// </summary>
public class CharacterMovingState : IState {
    private MovementComponent movementComponent;
    private CharacterAnimator animator;
    private Vector2 currentDirection;

    public CharacterMovingState(MovementComponent movementComponent, CharacterAnimator animator) {
        this.movementComponent = movementComponent;
        this.animator = animator;
    }

    public void Enter() {
        animator.SetMoving(true);
        animator.SetDashing(false);
    }

    public void Exit() { }


    /// <summary>
    /// Updates the movement direction.
    /// </summary>
    public void SetDirection(Vector2 direction) {
        currentDirection = direction;
        movementComponent.OnMove(direction);
    }
}
