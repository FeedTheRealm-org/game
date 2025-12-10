using UnityEngine;

/// <summary>
/// State for when the character is charging an attack.
/// </summary>
public class CharacterChargingAttackState : IState {
    private CharacterAnimator animator;

    public CharacterChargingAttackState(CharacterAnimator animator) {
        this.animator = animator;
    }

    public void Enter() {
        animator.SetMoving(false);
        animator.SetDashing(false);
        // TODO: Set charging animation if needed
    }

    public void Exit() {
        // No specific exit logic
    }

    public void Update() {
        // Handle charging logic, e.g., increase charge level
    }

    public void FixedUpdate() {
        // No physics updates needed
    }
}