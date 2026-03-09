using FTR.Core.Client.StateMachine;
using FTR.Core.Client.Utils;
using UnityEngine;

/// <summary>
/// State for when the character is dashing.
/// </summary>
public class CharacterDashingState : IMovementState
{
    private IStateMachine stateMachine;

    private MovementController movementController;

    public CharacterDashingState(IStateMachine sm, MovementController movementController)
    {
        this.movementController = movementController;
        this.stateMachine = sm;
    }

    public void Enter()
    {
        movementController.OnDash();
    }

    public void Exit() { }

    public void SetDirection(Vector2 direction)
    {
        IMovementState nextState;
        if (VectorTransformations.IsMovementMagnitude(direction))
        {
            nextState = stateMachine.GetMovementStateByType(typeof(CharacterMovingState));
            stateMachine.SetMovementState(nextState);
            stateMachine.CurrentMovementState.SetDirection(direction);
            return;
        }
        nextState = stateMachine.GetMovementStateByType(typeof(CharacterIdleState));
        stateMachine.SetMovementState(nextState);
    }

    public void Dispose()
    {
        stateMachine = null;
        movementController = null;
    }
}
