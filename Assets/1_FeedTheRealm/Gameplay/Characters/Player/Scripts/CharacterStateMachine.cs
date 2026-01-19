using System.Collections.Generic;
using Game.Core.Exceptions;
using Game.Core.StateMachine;
using UnityEngine;

/// <summary>
/// Manages the character's state machine, handling transitions based on input and state.
/// </summary>
public class CharacterStateMachine : MonoBehaviour, IStateMachine
{
    [SerializeField]
    private Logging.Logger logger;

    /* Components */
    [SerializeField]
    private MovementController movementController;

    [SerializeField]
    private DashComponent dashComponent;

    [SerializeField]
    private AttackComponent attackComponent;

    [SerializeField]
    private GroundCheckComponent groundCheckComponent;

    [SerializeField]
    private PlayerInteractComponent interactComponent;

    [SerializeField]
    private CharacterAnimator characterAnimator;

    /* States */
    public readonly Dictionary<System.Type, IMovementState> movementStates =
        new Dictionary<System.Type, IMovementState>();
    public readonly Dictionary<System.Type, IActionState> actionStates =
        new Dictionary<System.Type, IActionState>();

    /* State Layers - accessible by states */
    public IMovementState CurrentMovementState { get; private set; }
    public IActionState CurrentActionState { get; private set; }

    private Vector2 lastDirection;

    private bool isMovementBlocked;
    private bool isActionBlocked;

    private void Awake()
    {
        if (
            movementController == null
            || dashComponent == null
            || attackComponent == null
            || groundCheckComponent == null
            || interactComponent == null
            || characterAnimator == null
        )
        {
            throw new MissingFieldException(
                "One or more required components are missing in CharacterStateMachine."
            );
        }

        movementStates.Add(
            typeof(CharacterIdleState),
            new CharacterIdleState(this, movementController, characterAnimator)
        );
        movementStates.Add(
            typeof(CharacterMovingState),
            new CharacterMovingState(this, movementController, characterAnimator)
        );
        movementStates.Add(
            typeof(CharacterDashingState),
            new CharacterDashingState(this, dashComponent, characterAnimator)
        );
        actionStates.Add(
            typeof(CharacterAttackState),
            new CharacterAttackState(this, attackComponent, characterAnimator)
        );
        actionStates.Add(
            typeof(CharacterChargingAttackState),
            new CharacterChargingAttackState(this, characterAnimator)
        );
        actionStates.Add(
            typeof(CharacterInteractingState),
            new CharacterInteractingState(this, interactComponent, characterAnimator)
        );

        SetMovementState(movementStates[typeof(CharacterIdleState)]);
    }

    private void OnDestroy()
    {
        foreach (var state in movementStates.Values)
        {
            state.Dispose();
        }
        movementStates.Clear();

        foreach (var state in actionStates.Values)
        {
            state.Dispose();
        }
        actionStates.Clear();
    }

    public void SetMovementState(IMovementState newState)
    {
        CurrentMovementState?.Exit();
        CurrentMovementState = newState;
        CurrentMovementState?.Enter();
        if (!isMovementBlocked)
            CurrentMovementState.SetDirection(lastDirection);
    }

    public void SetActionState(IActionState newState)
    {
        CurrentActionState?.Exit();
        CurrentActionState = newState;
        CurrentActionState?.Enter();
    }

    public void ToggleBlockMovement(bool shouldBlock)
    {
        isMovementBlocked = shouldBlock;
        if (isMovementBlocked)
            SetMovementState(movementStates[typeof(CharacterIdleState)]);
    }

    public void ToggleBlockAction(bool shouldBlock)
    {
        isActionBlocked = shouldBlock;
    }

    public IMovementState GetMovementStateByType(System.Type type)
    {
        var ok = movementStates.TryGetValue(type, out IMovementState state);
        if (!ok)
            return null;
        return state;
    }

    public IActionState GetActionStateByType(System.Type type)
    {
        var ok = actionStates.TryGetValue(type, out IActionState state);
        if (!ok)
            return null;
        return state;
    }

    /// <summary>
    /// Handles movement input.
    /// </summary>
    public void OnMove(Vector2 direction)
    {
        if (isMovementBlocked)
            return;

        lastDirection = direction;
        CurrentMovementState.SetDirection(direction);
    }

    /// <summary>
    /// Handles dash input.
    /// </summary>
    public void OnDash()
    {
        if (isMovementBlocked)
            return;

        SetMovementState(movementStates[typeof(CharacterDashingState)]);
    }

    /// <summary>
    /// Handles attack down input (start charge or quick attack).
    /// </summary>
    public void OnAttack()
    {
        if (isActionBlocked)
            return;

        SetActionState(actionStates[typeof(CharacterAttackState)]);
    }

    /// <summary>
    /// Handles interaction input.
    /// </summary>
    public void OnInteract()
    {
        if (isActionBlocked)
            return;

        SetActionState(actionStates[typeof(CharacterInteractingState)]);
    }
}
