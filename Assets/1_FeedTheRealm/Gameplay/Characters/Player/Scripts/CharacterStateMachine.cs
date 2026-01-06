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
    private CharacterIdleState idleState;
    private CharacterMovingState movingState;
    private CharacterDashingState dashingState;
    private CharacterAttackState attackState;
    private CharacterChargingAttackState chargingAttackState;
    private CharacterInteractingState interactingState;

    /* State Layers - accessible by states */
    public IMovementState CurrentMovementState { get; private set; }
    public IActionState CurrentActionState { get; private set; }

    private Vector2 lastDirection = Vector2.zero;

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

        idleState = new CharacterIdleState(movementComponent, characterAnimator);
        movingState = new CharacterMovingState(movementComponent, characterAnimator);
        dashingState = new CharacterDashingState(dashComponent, characterAnimator);
        attackState = new CharacterAttackState(attackComponent, characterAnimator);
        chargingAttackState = new CharacterChargingAttackState(
            movementComponent,
            characterAnimator
        );
        interactingState = new CharacterInteractingState(interactComponent, characterAnimator);

        attackComponent.OnAttackFinished += OnAttackFinished;
        dashComponent.OnDashFinished += OnDashFinished;
        interactComponent.OnInteractFinished += OnInteractFinished;

        SetMovementState(idleState);
    }

    private void OnDisable()
    {
        attackComponent.OnAttackFinished -= OnAttackFinished;
        dashComponent.OnDashFinished -= OnDashFinished;
        interactComponent.OnInteractFinished -= OnInteractFinished;
    }

    public void SetMovementState(IMovementState newState)
    {
        CurrentMovementState?.Exit(this);
        CurrentMovementState = newState;
        CurrentMovementState?.Enter(this);
    }

    public void SetActionState(IActionState newState)
    {
        CurrentActionState?.Exit(this);
        CurrentActionState = newState;
        CurrentActionState?.Enter(this);
    }

    /// <summary>
    /// Handles movement input.
    /// </summary>
    public void OnMove(Vector2 direction)
    {
        lastDirection = direction;

        if (CurrentMovementState == dashingState || CurrentMovementState == chargingAttackState)
            return; // Cannot move while dashing or charging

        if (direction.sqrMagnitude > 0.01f)
        {
            if (CurrentMovementState != movingState)
            {
                SetMovementState(movingState);
            }
            movingState.SetDirection(direction);
        }
        else
        {
            if (CurrentMovementState != idleState)
            {
                SetMovementState(idleState);
            }
        }
    }

    /// <summary>
    /// Handles dash input.
    /// </summary>
    public void OnDash()
    {
        if (
            groundCheckComponent.IsGrounded
            && CurrentMovementState != dashingState
            && CurrentMovementState != chargingAttackState
        )
        {
            SetMovementState(dashingState);
        }
    }

    /// <summary>
    /// Handles attack down input (start charge or quick attack).
    /// </summary>
    public void OnAttack()
    {
        Debug.Log("State machine debug ATTACK");
        if (CurrentMovementState != chargingAttackState && CurrentActionState == null)
        {
            attackState.Enter(this);
        }
    }

    private void OnAttackFinished()
    {
        if (CurrentActionState == attackState)
        {
            attackState.Exit(this);
            CurrentActionState = null;
        }
    }

    private void OnDashFinished()
    {
        if (CurrentMovementState == dashingState)
        {
            if (lastDirection.sqrMagnitude > 0.01f)
            {
                SetMovementState(movingState);
                movingState.SetDirection(lastDirection);
            }
            else
            {
                SetMovementState(idleState);
            }
        }
    }

    /// <summary>
    /// Handles interaction input.
    /// </summary>
    public void OnInteract()
    {
        SetActionState(interactingState);
    }

    private void OnInteractFinished()
    {
        if (CurrentMovementState != interactingState)
            return;

        if (lastDirection.sqrMagnitude > 0.01f)
        {
            SetMovementState(movingState);
            movingState.SetDirection(lastDirection);
            return;
        }
        SetMovementState(idleState);
    }
}
