using FTR.Core.Client.Exceptions;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;

/// <summary>
/// Connects local player input to the state machine.
/// Client-only, no networking needed.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Inject]
    public PlayerInputReader inputReader;

    [Inject]
    private Logging.Logger logger;

    private CharacterStateMachine characterStateMachine;

    private bool isInitialized = false;

    public void Initialize(CharacterStateMachine characterStateMachine)
    {
        this.characterStateMachine = characterStateMachine;
        isInitialized = true;
        StartController();
    }

    public void StartController()
    {
        if (!isInitialized)
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
            inputReader.UseEvent += OnUseInput;
            inputReader.InteractEvent += OnInteractInput;
            return;
        }

        inputReader.DashEvent -= OnDashInput;
        inputReader.MoveEvent -= OnMoveInput;
        inputReader.UseEvent -= OnUseInput;
        inputReader.InteractEvent -= OnInteractInput;
    }

    private void OnUseInput()
    {
        if (Cursor.visible)
        {
            return;
        }

        characterStateMachine?.OnUse();
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
