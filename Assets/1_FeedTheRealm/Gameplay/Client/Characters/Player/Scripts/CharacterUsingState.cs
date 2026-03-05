using FTR.Core.Client.StateMachine;

public class CharacterUsingState : IActionState
{
    private IStateMachine stateMachine;

    private UseController useController;

    private CharacterAnimator animator;

    private bool attackTriggered;

    public CharacterUsingState(
        IStateMachine sm,
        UseController useController,
        CharacterAnimator animator
    )
    {
        this.stateMachine = sm;
        this.useController = useController;
        this.animator = animator;
    }

    public void Enter()
    { // TODO: rename to OnUseEnd
        animator.OnPlayAttackEnd += OnPlayAttackEnd;
        useController.Use();
    }

    public void Exit()
    {
        animator.OnPlayAttackEnd -= OnPlayAttackEnd;
    }

    private void OnPlayAttackEnd()
    {
        stateMachine.SetActionState(null);
    }

    public void Dispose()
    {
        stateMachine = null;
        useController = null;
        animator = null;
    }
}
