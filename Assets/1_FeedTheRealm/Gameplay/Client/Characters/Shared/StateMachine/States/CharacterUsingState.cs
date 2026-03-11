using FTR.Core.Client.StateMachine;

public class CharacterUsingState : IActionState
{
    private IStateMachine stateMachine;

    private UseController useController;

    private CharacterAnimator animator;

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
    {
        animator.OnUseAnimationEnd += OnUseAnimationEnd;
        useController.Use();
    }

    public void Exit()
    {
        animator.OnUseAnimationEnd -= OnUseAnimationEnd;
    }

    private void OnUseAnimationEnd()
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
