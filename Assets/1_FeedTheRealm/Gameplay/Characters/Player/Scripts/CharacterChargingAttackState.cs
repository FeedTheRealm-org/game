using Game.Core.StateMachine;

/// <summary>
/// State for when the character is charging an attack.
/// </summary>
public class CharacterChargingAttackState : IActionState
{
    private IStateMachine stateMachine;
    private CharacterAnimator animator;

    public CharacterChargingAttackState(IStateMachine sm, CharacterAnimator animator)
    {
        this.animator = animator;
        this.stateMachine = sm;
    }

    public void Enter()
    {
        // TODO: Charged attack
    }

    public void Exit() { }

    public void Dispose()
    {
        stateMachine = null;
        animator = null;
    }
}
