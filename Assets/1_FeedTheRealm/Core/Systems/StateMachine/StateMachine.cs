using System.Collections.Generic;

/// <summary>
/// Manages state transitions and updates for a state machine.
/// </summary>
public class StateMachine {

    private List<IState> currentStates;

    /// <summary>
    /// Initializes the state machine with a starting state.
    /// </summary>
    public void Initialize(List<IState> startingState) {
        currentStates = startingState;

        foreach (var state in currentStates) {
            state.Enter();
        }
    }

    /// <summary>
    /// Changes the current state to a new state.
    /// </summary>
    public void ChangeState(List<IState> newStates) {
        foreach (var state in currentStates) {
            state.Exit();
        }

        currentStates = newStates;

        foreach (var state in currentStates) {
            state.Enter();
        }
    }

    /// <summary>
    /// Updates the current state.
    /// </summary>
    public void Update() {
        foreach (var state in currentStates) {
            state.Update();
        }
    }

    /// <summary>
    /// Fixed update for the current state.
    /// </summary>
    public void FixedUpdate() {
        foreach (var state in currentStates) {
            state.FixedUpdate();
        }
    }
}
