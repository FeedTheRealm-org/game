using UnityEngine;

/// <summary>
/// State for when the character is charging an attack.
/// </summary>
public class CharacterChargingAttackState : IState
{
    private MovementComponent movementComponent;
    private CharacterAnimator animator;

    public CharacterChargingAttackState(MovementComponent movementComponent, CharacterAnimator animator)
    {
        this.movementComponent = movementComponent;
        this.animator = animator;
    }

    public void Enter()
    {
        animator.SetMoving(false);
        animator.SetDashing(false);
        movementComponent.OnMove(Vector2.zero); // Stop movement while charging
        // TODO: Set charging animation if needed
    }

    public void Exit()
    {
        // No specific exit logic
    }
}