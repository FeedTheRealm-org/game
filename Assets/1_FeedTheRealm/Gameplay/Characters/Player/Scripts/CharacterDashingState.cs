using UnityEngine;

/// <summary>
/// State for when the character is dashing.
/// </summary>
public class CharacterDashingState : IState {
    private DashComponent dashComponent;
    private CharacterAnimator animator;
    private bool dashTriggered;

    public CharacterDashingState(DashComponent dashComponent, CharacterAnimator animator) {
        this.dashComponent = dashComponent;
        this.animator = animator;
    }

    public void Enter() {
        animator.SetMoving(false);
        animator.SetDashing(true);
        dashTriggered = false;
    }

    public void Exit() {
        animator.SetDashing(false);
    }

    public void Update() {
        if (!dashTriggered) {
            dashComponent.OnDash();
            dashTriggered = true;
        }
        // Check if dash is finished - DashComponent handles the coroutine
        // For now, assume dash duration is handled by component
        // In a more advanced system, we might need to check dash status
    }

    public void FixedUpdate() {
        // Dashing physics handled by DashComponent
    }
}