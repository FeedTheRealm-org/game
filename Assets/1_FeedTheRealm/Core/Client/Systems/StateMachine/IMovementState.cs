using UnityEngine;

namespace Game.Core.Client.StateMachine
{
    /// <summary>
    /// Interface for defining movement layer states in a state machine.
    /// </summary>
    public interface IMovementState : IState
    {
        void SetDirection(Vector2 direction);
    }
}
