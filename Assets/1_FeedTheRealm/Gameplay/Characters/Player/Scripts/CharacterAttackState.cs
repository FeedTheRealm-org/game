using Game.Core.StateMachine;

public class CharacterAttackState : IActionState
{
    private IStateMachine stateMachine;

    private AttackComponent attackComponent;
    private CharacterAnimator animator;
    private bool attackTriggered;

    public CharacterAttackState(AttackComponent attackComponent, CharacterAnimator animator)
    {
        this.attackComponent = attackComponent;
        this.animator = animator;
    }

    public void Enter(IStateMachine stateMachine)
    {
        attackComponent.OnAttackFinished += OnAttackFinished;
        attackComponent.OnAttack();
        animator.SetAction(true);
        animator.PlayAttack();

        if (this.stateMachine == null)
            this.stateMachine = stateMachine;
    }

    public void Exit(IStateMachine stateMachine)
    {
        animator.SetAction(false);
        attackComponent.OnAttackFinished -= OnAttackFinished;

        if (this.stateMachine != null)
            this.stateMachine = null;
    }

    private void OnAttackFinished()
    {
        stateMachine?.SetActionState(null);
    }
}
