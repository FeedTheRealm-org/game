
/// <summary>
/// Interface for defining states in a state machine.
/// </summary>
public interface IState
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
