using UnityEngine;

/// <summary>
/// Manages the character's state machine, handling transitions based on input and state.
/// </summary>
public class CharacterStateMachine : MonoBehaviour
{
    [SerializeField]
    private MovementComponent movementComponent;

    [SerializeField]
    private DashComponent dashComponent;

    [SerializeField]
    private AttackComponent attackComponent;

    [SerializeField]
    private GroundCheckComponent groundCheckComponent;

    [SerializeField]
    private CharacterAnimator characterAnimator;

    [SerializeField]
    private PlayerInteractComponent interactComponent;

    private CharacterIdleState idleState;
    private CharacterMovingState movingState;
    private CharacterDashingState dashingState;
    private CharacterAttackState attackState;
    private CharacterChargingAttackState chargingAttackState;
    private CharacterInteractingState interactingState;

    private IState currentMovementState;
    private IState currentAttackState;
    private Vector2 lastDirection;

    private void Awake()
    {
        if (movementComponent == null)
            movementComponent = GetComponentInChildren<MovementComponent>();
        if (dashComponent == null)
            dashComponent = GetComponentInChildren<DashComponent>();
        if (attackComponent == null)
            attackComponent = GetComponentInChildren<AttackComponent>();
        if (groundCheckComponent == null)
            groundCheckComponent = GetComponentInChildren<GroundCheckComponent>();
        if (interactComponent == null)
            interactComponent = GetComponentInChildren<PlayerInteractComponent>();
        if (characterAnimator == null)
            characterAnimator = GetComponentInChildren<CharacterAnimator>();

        idleState = new CharacterIdleState(movementComponent, characterAnimator);
        movingState = new CharacterMovingState(movementComponent, characterAnimator);
        dashingState = new CharacterDashingState(dashComponent, characterAnimator);
        attackState = new CharacterAttackState(attackComponent, characterAnimator);
        chargingAttackState = new CharacterChargingAttackState(
            movementComponent,
            characterAnimator
        );
        interactingState = new CharacterInteractingState(interactComponent, characterAnimator);

        currentMovementState = idleState;
        currentAttackState = null;
        lastDirection = Vector2.zero;

        attackComponent.OnAttackFinished += OnAttackFinished;
        dashComponent.OnDashFinished += OnDashFinished;

        currentMovementState.Enter();
    }

    /// <summary>
    /// Handles movement input.
    /// </summary>
    public void OnMove(Vector2 direction)
    {
        lastDirection = direction;

        if (currentMovementState == dashingState || currentMovementState == chargingAttackState)
            return; // Cannot move while dashing or charging

        if (direction.sqrMagnitude > 0.01f)
        {
            if (currentMovementState != movingState)
            {
                ChangeMovementState(movingState);
            }
            movingState.SetDirection(direction);
        }
        else
        {
            if (currentMovementState != idleState)
            {
                ChangeMovementState(idleState);
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
            && currentMovementState != dashingState
            && currentMovementState != chargingAttackState
        )
        {
            ChangeMovementState(dashingState);
        }
    }

    /// <summary>
    /// Handles attack down input (start charge or quick attack).
    /// </summary>
    public void OnAttack()
    {
        //Debug.Log("State machine debug ATTACK");
        if (currentMovementState != chargingAttackState && currentAttackState == null)
        {
            attackState.Enter();
        }
    }

    private void ChangeMovementState(IState newState)
    {
        currentMovementState.Exit();
        currentMovementState = newState;
        currentMovementState.Enter();
    }

    private void OnAttackFinished()
    {
        if (currentAttackState == attackState)
        {
            attackState.Exit();
            currentAttackState = null;
        }
    }

    private void OnDashFinished()
    {
        if (currentMovementState == dashingState)
        {
            if (lastDirection.sqrMagnitude > 0.01f)
            {
                ChangeMovementState(movingState);
                movingState.SetDirection(lastDirection);
            }
            else
            {
                ChangeMovementState(idleState);
            }
        }
    }

    /// <summary>
    /// Handles interaction input.
    /// </summary>
    public void OnInteract()
    {
        ChangeMovementState(interactingState);
    }
}
