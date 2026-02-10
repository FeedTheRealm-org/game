using System;

namespace FTR.Core.Client.StateMachine
{
    /// <summary>
    /// Interface for defining states in a state machine.
    /// </summary>
    public interface IState : IDisposable
    {
        /// <summary>
        /// Called when entering the state.
        /// </summary>
        void Enter();

        /// <summary>
        /// Called when exiting the state.
        /// </summary>
        void Exit();
    }
}
