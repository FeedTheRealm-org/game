using Game.Core.StateMachine;

/// <summary>
/// State for when the character is charging an attack.
/// </summary>
public class CharacterChargingAttackState : IActionState
{
    private IStateMachine stateMachine;
    private MovementComponent movementComponent;
    private CharacterAnimator animator;

    public CharacterChargingAttackState(
        IStateMachine sm,
        MovementComponent movementComponent,
        CharacterAnimator animator
    )
    {
        this.movementComponent = movementComponent;
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
        movementComponent = null;
        animator = null;
    }
}
