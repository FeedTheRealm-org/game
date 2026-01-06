namespace Game.Core.StateMachine
{
    /// <summary>
    /// Interface for defining a state machine base methods.
    /// </summary>
    public interface IStateMachine
    {
        /// <summary>
        /// Sets the current state of the state machine.
        /// </summary>
        void SetMovementState(IMovementState state);

        /// <summary>
        /// Sets the current action state of the state machine.
        /// </summary>
        void SetActionState(IActionState state);
    }
}
