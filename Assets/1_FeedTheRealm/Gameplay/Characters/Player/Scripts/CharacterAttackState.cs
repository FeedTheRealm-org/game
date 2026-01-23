using Game.Core.StateMachine;

public class CharacterAttackState : IActionState
{
    private IStateMachine stateMachine;

    private AttackComponent attackComponent;
    private CharacterAnimator animator;
    private bool attackTriggered;

    public CharacterAttackState(
        IStateMachine sm,
        AttackComponent attackComponent,
        CharacterAnimator animator
    )
    {
        this.attackComponent = attackComponent;
        this.animator = animator;
        this.stateMachine = sm;
    }

    public void Enter()
    {
        attackComponent.OnAttackFinished += OnAttackFinished;
        attackComponent.OnAttack();
        animator.SetAction(true);
        animator.PlayAttack();
    }

    public void Exit()
    {
        animator.SetAction(false);
        attackComponent.OnAttackFinished -= OnAttackFinished;
    }

    private void OnAttackFinished()
    {
        stateMachine.SetActionState(null);
    }

    public void Dispose()
    {
        stateMachine = null;
        attackComponent = null;
        animator = null;
    }
}
