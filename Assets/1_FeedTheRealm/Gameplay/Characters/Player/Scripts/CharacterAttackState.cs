
using UnityEngine;

public class CharacterAttackState : IState {
    private AttackComponent attackComponent;
    private CharacterAnimator animator;
    private bool attackTriggered;

    public CharacterAttackState(AttackComponent attackComponent, CharacterAnimator animator) {
        this.attackComponent = attackComponent;
        this.animator = animator;
    }

    public void Enter() {
        attackTriggered = false;
    }

    public void Exit() {
        // No specific exit logic
    }

    public void Update() {
        if (!attackTriggered) {
            attackComponent.OnAttack();
            animator.PlayAttack();
            attackTriggered = true;
        }
        // AttackComponent handles the cooldown coroutine
    }

    public void FixedUpdate() {
        // No physics updates needed
    }
}
