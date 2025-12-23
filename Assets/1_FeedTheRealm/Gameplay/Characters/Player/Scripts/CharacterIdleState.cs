using UnityEngine;

/// <summary>
/// State for when the character is idle (not moving).
/// </summary>
public class CharacterIdleState : IState
{
    private MovementComponent movementComponent;
    private CharacterAnimator animator;

    public CharacterIdleState(MovementComponent movementComponent, CharacterAnimator animator)
    {
        this.movementComponent = movementComponent;
        this.animator = animator;
    }

    public void Enter()
    {
        animator.SetMoving(false);
        animator.SetDashing(false);
        movementComponent.OnMove(Vector2.zero);
    }

    public void Exit() { }
}
