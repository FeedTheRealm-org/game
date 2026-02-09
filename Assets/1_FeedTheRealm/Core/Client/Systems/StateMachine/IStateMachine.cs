namespace Game.Core.Client.StateMachine
{
    /// <summary>
    /// Interface for defining a state machine base methods.
    /// </summary>
    public interface IStateMachine
    {
        /// <summary>
        /// Current movement state of the state machine.
        /// </summary>
        IMovementState CurrentMovementState { get; }

        /// <summary>
        /// Current action state of the state machine.
        /// </summary>
        IActionState CurrentActionState { get; }

        /// <summary>
        /// Sets the current state of the state machine.
        /// </summary>
        void SetMovementState(IMovementState state);

        /// <summary>
        /// Sets the current action state of the state machine.
        /// </summary>
        void SetActionState(IActionState state);

        /// <summary>
        /// Blocks/Unblocks movement state layer.
        /// </summary>
        void ToggleBlockMovement(bool shoudBlock);

        /// <summary>
        /// Blocks/Unblocks action state layer.
        /// </summary>
        void ToggleBlockAction(bool shouldBlock);

        /// <summary>
        /// Gets movement state instance from type.
        /// </summary>
        IMovementState GetMovementStateByType(System.Type type);

        /// <summary>
        /// Gets action state instance from type.
        /// </summary>
        IActionState GetActionStateByType(System.Type type);
    }
}
