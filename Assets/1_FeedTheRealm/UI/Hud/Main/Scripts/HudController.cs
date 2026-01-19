using System;
using System.Threading;
using System.Threading.Tasks;
using FeedTheRealm.UI;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages the HUD elements and their interactions.
/// </summary>
[RequireComponent(typeof(HudFastUseSlotsController))]
public class HudController : MonoBehaviour
{
    [SerializeField]
    private Stamina staminaData;

    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private Session.Session session;

    [Header("Input")]
    [SerializeField]
    [Tooltip(
        "PlayerInputReader used for HUD quickslots (should match the one used by the player/game scene)."
    )]
    private PlayerInputReader inputReader;

    [Header("Wiring")]
    [SerializeField]
    [Tooltip(
        "Registry asset used so other prefabs (Inventory) can access HUD quickslots without scene references."
    )]
    private HudFastUseSlotsRegistry hudFastSlotsRegistry;

    // Containers
    private VisualElement _characterData;
    private VisualElement _fastUseSlotsContainer;
    private Label _nameLabel;

    // Currency UI
    private Label _goldAmountLabel;

    // Progress Bars
    private ProgressBar _staminaBar;

    private HUDGoldBinder _goldBinder;
    private CancellationTokenSource _bindCts;

    private HudFastUseSlotsController _fastUseSlotsController;

    void Start()
    {
        // Only run HUD on clients. If this instance is a dedicated server (server active
        // but no client), disable early to avoid server-side UI lookups and warnings.
        if (NetworkServer.active && !NetworkClient.active)
        {
            logger.Log("HUDController disabled: dedicated server (no client active)", this);
            enabled = false;
            return;
        }

        var root = GetComponent<UIDocument>().rootVisualElement;

        _characterData = root.Q<VisualElement>("CharacterData");
        _fastUseSlotsContainer = root.Q<VisualElement>("FastUseSlotsContainer");
        if (_characterData == null || _fastUseSlotsContainer == null)
        {
            logger.Log(
                "CharacterData or FastUseSlotsContainer not found in the UI Document.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        _fastUseSlotsController = GetComponent<HudFastUseSlotsController>();
        _fastUseSlotsController.SetLogger(logger);
        _fastUseSlotsController.SetRegistry(hudFastSlotsRegistry);
        if (inputReader == null)
        {
            logger?.Log(
                "[HUD] PlayerInputReader is not assigned in HudController; quickslot hotkeys will not work.",
                this,
                Logging.LogType.Warning
            );
        }
        _fastUseSlotsController.SetInputReader(inputReader);
        _fastUseSlotsController.OnSlotActivated -= HandleFastUseSlotActivated;
        _fastUseSlotsController.OnSlotActivated += HandleFastUseSlotActivated;

        SpriteLoader.OnSpriteLoaderReady += HandleSpriteLoaderReady;

        // Important: Register fastSlots controller in the registry so InventoryController can access it
        if (hudFastSlotsRegistry != null)
        {
            hudFastSlotsRegistry.Register(_fastUseSlotsController);
        }

        _staminaBar = _characterData.Q<ProgressBar>("StaminaBar");
        if (_staminaBar == null)
        {
            logger.Log("StaminaBar not found in CharacterData.", this, Logging.LogType.Error);
            return;
        }

        _staminaBar.value = _staminaBar.highValue;

        _nameLabel = _characterData.Q<Label>("Username");
        if (_nameLabel != null && session != null)
        {
            _nameLabel.text = session.CharacterName;
        }

        var currencyContainer = _characterData.Q<VisualElement>("CurrencyContainer");
        _goldAmountLabel = currencyContainer?.Q<Label>("GoldAmount");

        if (_goldAmountLabel == null)
        {
            logger.Log(
                "GoldAmount label not found in HUD. Gold UI will not be updated.",
                this,
                Logging.LogType.Warning
            );
        }
        else
        {
            // Initialize to 0 until we get data from the player
            _goldAmountLabel.text = "0";
        }

        _goldBinder = GetComponent<HUDGoldBinder>();
        if (_goldBinder == null)
        {
            _goldBinder = gameObject.AddComponent<HUDGoldBinder>();
        }
        _goldBinder.SetLogger(logger);

        _bindCts = new CancellationTokenSource();
        _ = StartBindingAsync(_bindCts.Token);

        registerButtonCallbacks();
    }

    private void HandleFastUseSlotActivated(int slotIndex, string itemId)
    {
        // Placeholder: for now just log. Game logic can subscribe to HudFastUseSlotsController.OnSlotActivated.
        logger?.Log($"[HUD] FastUse Slot{slotIndex} activated => itemId={itemId}", this);
    }

    /// <summary>
    /// Handles when a SpriteLoader becomes ready and assigns it to the fast use slots controller.
    /// </summary>
    private void HandleSpriteLoaderReady(SpriteLoader spriteLoader)
    {
        _fastUseSlotsController.SetSpriteLoader(spriteLoader);
        logger?.Log("[HUD] SpriteLoader assigned via event", this);
    }

    private async Task StartBindingAsync(CancellationToken token)
    {
        const float timeout = 10f;

        float elapsed = 0f;
        while (!NetworkClient.active && elapsed < timeout)
        {
            if (token.IsCancellationRequested)
                return;
            try
            {
                await Task.Delay(500, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            elapsed += 0.5f;
        }

        bool bound = false;
        try
        {
            bound = await _goldBinder.BindAsync(timeout - elapsed, token);
        }
        catch (OperationCanceledException) { }

        if (!bound)
        {
            logger.Log(
                "HUDController could not find PlayerGold component after waiting; gold UI will stay at initial value.",
                this,
                Logging.LogType.Warning
            );
        }
        else
        {
            _goldBinder.OnGoldChanged += HandleGoldChanged;
        }
    }

    /// <summary>
    /// Registers click event callbacks for buttons in the HUD.
    /// </summary>
    private void registerButtonCallbacks()
    {
        var _characterIconButton = _characterData.Q<Button>("CharacterIcon");
        _characterIconButton?.RegisterCallback<ClickEvent>(ev =>
        {
            logger.Log("Character Icon Clicked", this);
        });
    }

    private void OnEnable()
    {
        if (staminaData != null)
        {
            staminaData.OnStaminaChanged += handleStaminaChange;
        }
    }

    private void OnDisable()
    {
        if (staminaData != null)
        {
            staminaData.OnStaminaChanged -= handleStaminaChange;
        }

        if (_goldBinder != null)
        {
            _goldBinder.OnGoldChanged -= HandleGoldChanged;
            _goldBinder.Unbind();
        }

        if (_bindCts != null)
        {
            _bindCts.Cancel();
            _bindCts.Dispose();
            _bindCts = null;
        }

        if (_fastUseSlotsController != null)
        {
            _fastUseSlotsController.OnSlotActivated -= HandleFastUseSlotActivated;
        }

        SpriteLoader.OnSpriteLoaderReady -= HandleSpriteLoaderReady;
    }

    /// <summary>
    /// Handles changes in stamina and updates the HUD accordingly.
    /// </summary>
    private void handleStaminaChange(float value)
    {
        if (_staminaBar != null)
        {
            // Adjust for a stamina greater or lower than progress bar max (100).
            _staminaBar.value = value * _staminaBar.highValue / staminaData.MaxStamina;
        }
    }

    /// <summary>
    /// Updates the HUD gold label when the player's gold changes.
    /// </summary>
    private void HandleGoldChanged(int newGoldValue)
    {
        if (_goldAmountLabel != null)
        {
            _goldAmountLabel.text = newGoldValue.ToString();
        }
    }
}
