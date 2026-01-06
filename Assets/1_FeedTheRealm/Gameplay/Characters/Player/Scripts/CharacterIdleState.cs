using Game.Core.StateMachine;
using UnityEngine;

/// <summary>
/// State for when the character is idle (not moving).
/// </summary>
public class CharacterIdleState : IMovementState
{
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
    }

    public void Exit(IStateMachine stateMachine) { }

    public void SetDirection(Vector2 direction) { }
}
