
public class CharacterAttackState : IState {
    private AttackComponent attackComponent;
    private CharacterAnimator animator;
    private bool attackTriggered;

    public CharacterAttackState(AttackComponent attackComponent, CharacterAnimator animator) {
        this.attackComponent = attackComponent;
        this.animator = animator;
    }

    public void Enter() {
        attackComponent.OnAttack();
        animator.SetAction(true);
        animator.PlayAttack();
    }

    public void Exit() {
        animator.SetAction(false);
    }
}
