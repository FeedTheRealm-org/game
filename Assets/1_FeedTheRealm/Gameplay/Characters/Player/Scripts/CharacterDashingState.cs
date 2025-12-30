/// <summary>
/// State for when the character is dashing.
/// </summary>
public class CharacterDashingState : IState
{
    private DashComponent dashComponent;
    private CharacterAnimator animator;
    private bool dashTriggered;

    public CharacterDashingState(DashComponent dashComponent, CharacterAnimator animator)
    {
        this.dashComponent = dashComponent;
        this.animator = animator;
    }

    public void Enter()
    {
        animator.SetMoving(false);
        animator.SetDashing(true);
        dashComponent.OnDash();
    }

    public void Exit()
    {
        animator.SetDashing(false);
    }
}
