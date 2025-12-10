using UnityEngine;

/// <summary>
/// State for when the character is idle (not moving).
/// </summary>
public class CharacterIdleState : IState {
    private CharacterAnimator animator;

    public CharacterIdleState(CharacterAnimator animator) {
        this.animator = animator;
    }

    public void Enter() {
        animator.SetMoving(false);
        animator.SetDashing(false);
    }

    public void Exit() {
        // No specific exit logic
    }

    public void Update() {
        // Idle state doesn't need update logic
    }

    public void FixedUpdate() {
        // No physics updates needed
    }
}