using FTR.Core.Client.EventChannels;
using FTR.Core.Client.EventChannels.Chat;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Client.EventChannels.Portal;
using FTR.Core.Client.EventChannels.Shop;
using FTR.Core.Client.Exceptions;
using FTR.Core.Client.Input;
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
    private CursorManager cursorManager;

    [Inject]
    private CameraManager cameraManager;

    private CharacterStateMachine characterStateMachine;

    private bool isInitialized = false;

    // TODO: these are all UI realted states, we should
    // unify this into a single UI state manager or something similar to avoid having a million bools here.
    private bool isInventoryOpen = false;
    private bool isShopOpen = false;
    private bool isChatOpen = false;
    private bool isPortalOpen = false;

    private bool isUiOpen => isInventoryOpen || isShopOpen || isChatOpen || isPortalOpen;

    public void Initialize(CharacterStateMachine characterStateMachine)
    {
        this.characterStateMachine = characterStateMachine;
        isInitialized = true;

        if (inventoryToggleEvent != null)
            inventoryToggleEvent.OnRaised += OnInventoryToggled;

        if (shopToggleEvent != null)
            shopToggleEvent.OnRaised += OnShopToggled;

        if (chatToggleEvent != null)
            chatToggleEvent.OnRaised += OnChatToggled;

        if (portalToggleEvent != null)
            portalToggleEvent.OnRaised += OnPortalToggled;

        StartController();
    }

    private void OnInventoryToggled(bool isOpen)
    {
        isInventoryOpen = isOpen;
        UpdateCursorState();
    }

    private void OnShopToggled(bool isOpen)
    {
        isShopOpen = isOpen;
        UpdateCursorState();
    }

    private void OnChatToggled(bool isOpen)
    {
        isChatOpen = isOpen;
        UpdateCursorState();
    }

    private void OnPortalToggled(bool isOpen)
    {
        isPortalOpen = isOpen;
        UpdateCursorState();
    }

    private void UpdateCursorState()
    {
        bool shouldShowCursor = isInventoryOpen || isShopOpen || isChatOpen || isPortalOpen;
        cursorManager.ToggleCursorBlock(!shouldShowCursor);
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

        if (inventoryToggleEvent != null)
            inventoryToggleEvent.OnRaised -= OnInventoryToggled;

        if (shopToggleEvent != null)
            shopToggleEvent.OnRaised -= OnShopToggled;

        if (chatToggleEvent != null)
            chatToggleEvent.OnRaised -= OnChatToggled;

        if (portalToggleEvent != null)
            portalToggleEvent.OnRaised -= OnPortalToggled;
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
        if (isInventoryOpen || isShopOpen || isChatOpen || isPortalOpen)
        {
            return;
        }

        characterStateMachine?.OnUse();
    }

    private void OnMoveInput(Vector2 vec)
    {
        if (isUiOpen)
        {
            characterStateMachine?.OnMove(Vector2.zero);
            return;
        }
        characterStateMachine?.OnMove(vec);
    }

    private void OnDashInput()
    {
        if (isUiOpen)
        {
            return;
        }

        characterStateMachine?.OnDash();
    }

    private void OnInteractInput()
    {
        if (isInventoryOpen || isChatOpen || isPortalOpen)
        {
            return;
        }

        characterStateMachine?.OnInteract();
    }
}
