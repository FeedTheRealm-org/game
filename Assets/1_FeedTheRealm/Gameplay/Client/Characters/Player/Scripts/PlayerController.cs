using FTR.Core.Client.EventChannels;
using FTR.Core.Client.EventChannels.Chat;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Client.EventChannels.Shop;
using FTR.Core.Client.Exceptions;
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
    private Logging.Logger logger;

    private CharacterStateMachine characterStateMachine;

    private bool isInitialized = false;
    private bool isInventoryOpen = false;
    private bool isShopOpen = false;
    private bool isChatOpen = false;

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

    private void UpdateCursorState()
    {
        bool shouldShowCursor = isInventoryOpen || isShopOpen || isChatOpen;
        Cursor.visible = shouldShowCursor;
        Cursor.lockState = shouldShowCursor ? CursorLockMode.None : CursorLockMode.Locked;
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

        if (inventoryToggleEvent != null)
            inventoryToggleEvent.OnRaised -= OnInventoryToggled;

        if (shopToggleEvent != null)
            shopToggleEvent.OnRaised -= OnShopToggled;

        if (chatToggleEvent != null)
            chatToggleEvent.OnRaised -= OnChatToggled;
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
        if (isInventoryOpen || isShopOpen || isChatOpen)
        {
            return;
        }

        characterStateMachine?.OnUse();
    }

    private void OnMoveInput(Vector2 vec)
    {
        if (isChatOpen)
        {
            return;
        }

        characterStateMachine?.OnMove(vec);
    }

    private void OnDashInput()
    {
        if (isChatOpen)
        {
            return;
        }

        characterStateMachine?.OnDash();
    }

    private void OnInteractInput()
    {
        if (isInventoryOpen || isShopOpen || isChatOpen)
        {
            return;
        }

        characterStateMachine?.OnInteract();
    }
}
