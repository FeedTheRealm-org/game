
/// <summary>
/// Interface for defining states in a state machine.
/// </summary>
public interface IState {

    /// <summary>
    /// Called when entering the state.
    /// </summary>
    void Enter();

    /// <summary>
    /// Called on each update frame while in the state.
    /// </summary>
    void Update();

    /// <summary>
    /// Called on each fixed update frame while in the state.
    /// </summary>
    void FixedUpdate();

    /// <summary>
    /// Called when exiting the state.
    /// </summary>
    void Exit();
}
