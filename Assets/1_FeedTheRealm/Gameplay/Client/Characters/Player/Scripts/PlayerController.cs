using FTR.Core.Client.Exceptions;
using Mirror;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;

/// <summary>
/// Connects local player input to the state machine.
/// </summary>
public class PlayerController : NetworkBehaviour
{
    [Inject]
    public PlayerInputReader inputReader;

    [Inject]
    private Logging.Logger logger;

    private CharacterStateMachine characterStateMachine;

    private bool isInitialized = false;
    private bool isStarted = false;

    public void Initialize(CharacterStateMachine characterStateMachine)
    {
        this.characterStateMachine = characterStateMachine;
        isInitialized = true;
        StartController();
    }

    public override void OnStartAuthority()
    {
        isStarted = true;
        StartController();
    }

    public void StartController()
    {
        if (!isLocalPlayer)
            return;
        else if (!isInitialized || !isStarted)
            return;

        if (inputReader == null)
            throw new MissingFieldException(nameof(inputReader), nameof(PlayerController));
        if (characterStateMachine == null)
            throw new MissingFieldException(
                nameof(characterStateMachine),
                nameof(PlayerController)
            );

        var cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        cinemachineCamera.Target.TrackingTarget = transform;

        ToggleRegisterInputs(true);
    }

    public override void OnStopAuthority()
    {
        ToggleRegisterInputs(false);
    }

    public void OnDestroy()
    {
        ToggleRegisterInputs(false);
    }

    private void ToggleRegisterInputs(bool register)
    {
        logger.Log($"PlayerController ToggleRegisterInputs: {register}", this);
        if (register)
        {
            inputReader.DashEvent += OnDashInput;
            inputReader.MoveEvent += OnMoveInput;
            inputReader.AttackEvent += OnAttackInput;
            inputReader.InteractEvent += OnInteractInput;
            return;
        }

        inputReader.DashEvent -= OnDashInput;
        inputReader.MoveEvent -= OnMoveInput;
        inputReader.AttackEvent -= OnAttackInput;
        inputReader.InteractEvent -= OnInteractInput;
    }

    private void OnAttackInput()
    {
        if (Cursor.visible)
        {
            return;
        }

        characterStateMachine?.OnAttack();
    }

    private void OnMoveInput(Vector2 vec)
    {
        // if (Cursor.visible)
        // {
        //     return;
        // }
        // TODO: remove these if and make the state machine know via events when it can execute inputs or not (e.g. Hud manager events).

        logger.Log($"PlayerController OnMoveInput: {vec}", this);
        characterStateMachine?.OnMove(vec);
    }

    private void OnDashInput()
    {
        if (Cursor.visible)
        {
            return;
        }

        characterStateMachine?.OnDash();
    }

    private void OnInteractInput()
    {
        if (Cursor.visible)
        {
            return;
        }

        characterStateMachine?.OnInteract();
    }
}
