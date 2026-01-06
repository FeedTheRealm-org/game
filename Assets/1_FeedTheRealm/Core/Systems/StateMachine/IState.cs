namespace Game.Core.StateMachine
{
    /// <summary>
    /// Interface for defining states in a state machine.
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Called when entering the state.
        /// </summary>
        void Enter(IStateMachine stateMachine);

        /// <summary>
        /// Called when exiting the state.
        /// </summary>
        void Exit(IStateMachine stateMachine);
    }
}
