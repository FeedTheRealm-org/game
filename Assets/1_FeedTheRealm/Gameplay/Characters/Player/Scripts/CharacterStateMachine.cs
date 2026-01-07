using System.Collections.Generic;
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
    private MovementComponent movementComponent;
    private DashComponent dashComponent;
    private AttackComponent attackComponent;
    private GroundCheckComponent groundCheckComponent;
    private CharacterAnimator characterAnimator;
    private PlayerInteractComponent interactComponent;

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
        movementComponent = GetComponentInChildren<MovementComponent>();
        dashComponent = GetComponentInChildren<DashComponent>();
        attackComponent = GetComponentInChildren<AttackComponent>();
        groundCheckComponent = GetComponentInChildren<GroundCheckComponent>();
        interactComponent = GetComponentInChildren<PlayerInteractComponent>();
        characterAnimator = GetComponentInChildren<CharacterAnimator>();

        if (movementComponent == null)
            logger.Log(
                "MovementComponent not found on CharacterStateMachine.",
                this,
                Logging.LogType.Error
            );
        if (dashComponent == null)
            logger.Log(
                "DashComponent not found on CharacterStateMachine.",
                this,
                Logging.LogType.Error
            );
        if (attackComponent == null)
            logger.Log(
                "AttackComponent not found on CharacterStateMachine.",
                this,
                Logging.LogType.Error
            );
        if (groundCheckComponent == null)
            logger.Log(
                "GroundCheckComponent not found on CharacterStateMachine.",
                this,
                Logging.LogType.Error
            );
        if (interactComponent == null)
            logger.Log(
                "PlayerInteractComponent not found on CharacterStateMachine.",
                this,
                Logging.LogType.Error
            );
        if (characterAnimator == null)
            logger.Log(
                "CharacterAnimator not found on CharacterStateMachine.",
                this,
                Logging.LogType.Error
            );

        movementStates.Add(
            typeof(CharacterIdleState),
            new CharacterIdleState(this, movementComponent, characterAnimator)
        );
        movementStates.Add(
            typeof(CharacterMovingState),
            new CharacterMovingState(this, movementComponent, characterAnimator)
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
            new CharacterChargingAttackState(this, movementComponent, characterAnimator)
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
