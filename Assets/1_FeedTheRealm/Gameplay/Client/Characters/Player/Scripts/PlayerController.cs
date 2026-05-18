using FTR.Core.Client.EventChannels;
using FTR.Core.Client.EventChannels.Chat;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Client.EventChannels.Portal;
using FTR.Core.Client.EventChannels.Shop;
using FTR.Core.Client.Exceptions;
using FTR.Core.Client.Managers;
using FTR.Gameplay.Client.Characters.Shared.StateMachine;
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
    private InventoryToggleEvent inventoryToggleEvent;

    [Inject]
    private ShopToggleEvent shopToggleEvent;

    [Inject]
    private ChatToggleEvent chatToggleEvent;

    [Inject]
    private PortalToggleEvent portalToggleEvent;

    [Inject]
    private Logging.Logger logger;

    [Inject]
    private CameraManager cameraManager;

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

        cameraManager.TrackTarget(transform);

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
        characterStateMachine?.OnUse();
    }

    private void OnMoveInput(Vector2 vec)
    {
        characterStateMachine?.OnMove(vec);
    }

    private void OnDashInput()
    {
        characterStateMachine?.OnDash();
    }

    private void OnInteractInput()
    {
        characterStateMachine?.OnInteract();
    }
}
