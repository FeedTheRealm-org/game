using UnityEngine;

/// <summary>
/// State for when the character is charging an attack.
/// </summary>
public class CharacterChargingAttackState : IState {
    private MovementComponent movementComponent;
    private CharacterAnimator animator;

    public CharacterChargingAttackState(MovementComponent movementComponent, CharacterAnimator animator) {
        this.movementComponent = movementComponent;
        this.animator = animator;
    }

    public void Enter() {
        // TODO: Charged attack
    }

    public void Exit() {
    }
}
