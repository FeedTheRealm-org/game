using FTR.Core.Client.StateMachine;
using FTR.Core.Client.Utils;
using UnityEngine;

/// <summary>
/// State for when the character is moving.
/// </summary>
public class CharacterMovingState : IMovementState
{
    private IStateMachine stateMachine;

    private MovementController movementController;

    public CharacterMovingState(IStateMachine sm, MovementController movementController)
    {
        this.movementController = movementController;
        this.stateMachine = sm;
    }

    public void Enter() { }

    public void Exit() { }

    public void SetDirection(Vector2 direction)
    {
        if (!VectorTransformations.IsMovementMagnitude(direction))
        {
            var nextState = stateMachine.GetMovementStateByType(typeof(CharacterIdleState));
            stateMachine.SetMovementState(nextState);
            return;
        }

        movementController.SetDirection(direction);
    }

    public void Dispose()
    {
        stateMachine = null;
        movementController = null;
    }
}
