using System;
using System.Collections.Generic;
using FeedTheRealm.UI.Hud;
using FeedTheRealm.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controls HUD fast-use slots (Slot1..Slot5): assignment via inventory drag-drop and activation via keys 1..5.
/// Has tooltip support and item swapping between slots. Slots are generated dynamically at runtime (set on unity editor).
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class HudFastUseSlotsController : BaseSlotContainer, IDropTarget
{
    public event Action<int, string> OnSlotActivated;

    private PlayerInputReader inputReader;
    private HudFastUseSlotsRegistry registry;
    private SpriteLoader spriteLoader;

    private readonly Dictionary<int, Button> _slotButtons = new();

    private VisualElement _fastUseSlotsContainer;
    private PlayerInputReader _boundInputReader;

    protected override void Awake()
    {
        // slotCount is configurable in Unity Inspector (default: 5, max: 9 for keyboard keys 1-9)
        base.Awake();

        // Enforce maximum of 9 slots (keyboard keys 1-9)
        if (slotCount > 9)
        {
            Debug.LogWarning(
                $"[HUD] slotCount ({slotCount}) exceeds maximum of 9. Clamping to 9.",
                this
            );
            slotCount = 9;
        }
    }

    public void SetLogger(Logging.Logger log)
    {
        logger = log;
    }

    public void SetRegistry(HudFastUseSlotsRegistry newRegistry)
    {
        if (ReferenceEquals(registry, newRegistry))
            return;

        // Unregister from old registry.
        registry?.Unregister(this);

        registry = newRegistry;

        // Register into the new registry immediately if active.
        if (isActiveAndEnabled)
        {
            registry?.Register(this);
        }
    }

    protected override void Start()
    {
        base.Start();
        TryInitializeIfNeeded();
        TryBindInputReader();
        registry?.Register(this);
    }

    private void OnEnable()
    {
        TryBindInputReader();
        registry?.Register(this);
    }

    private void OnDisable()
    {
        UnbindInputReader();
        registry?.Unregister(this);
    }

    public void SetInputReader(PlayerInputReader reader)
    {
        if (ReferenceEquals(inputReader, reader))
            return;

        inputReader = reader;
        TryBindInputReader();
    }

    public void SetSpriteLoader(SpriteLoader loader)
    {
        spriteLoader = loader;
        logger?.Log($"[HUD] SpriteLoader assigned: {(loader != null ? "SUCCESS" : "NULL")}", this);
    }

    private void TryBindInputReader()
    {
        if (inputReader == null)
            return;

        if (ReferenceEquals(_boundInputReader, inputReader))
            return;

        UnbindInputReader();

        _boundInputReader = inputReader;
        _boundInputReader.QuickSlotEvent += HandleQuickSlotPressed;
    }

    public bool IsOnSamePanel(IPanel panel)
    {
        TryInitializeIfNeeded();
        return root != null && ReferenceEquals(root.panel, panel);
    }

    private void UnbindInputReader()
    {
        if (_boundInputReader == null)
            return;

        _boundInputReader.QuickSlotEvent -= HandleQuickSlotPressed;
        _boundInputReader = null;
    }

    private void HandleQuickSlotPressed(int slotIndex)
    {
        // Slot index is 1-based.
        ActivateSlot(slotIndex);
    }

    /// <summary>
    /// Lazy initialization: generates HUD slots dynamically if not already initialized.
    /// Called multiple times as a guard, but only initializes once (early return if already done).
    /// Process: Query container → clear UXML remnants → generate buttons via SlotUIGenerator →
    /// register in SlotManager → attach hover events for tooltips.
    /// </summary>
    private void TryInitializeIfNeeded()
    {
        // Early return if already initialized (most common path after Start())
        if (root != null && _fastUseSlotsContainer != null && _slotButtons.Count > 0)
            return;

        if (root == null)
            return;

        _fastUseSlotsContainer = root.Q<VisualElement>("FastUseSlotsContainer");
        if (_fastUseSlotsContainer == null)
            return;

        // Clear any existing slots (in case UXML has hardcoded slots)
        SlotUIGenerator.ClearContainer(_fastUseSlotsContainer);

        // Generate slots dynamically
        _slotButtons.Clear();
        var generatedButtons = SlotUIGenerator.GenerateButtonSlots(
            _fastUseSlotsContainer,
            slotCount,
            slotNamePrefix,
            useOneBasedNaming,
            "fast-use-slot",
            focusable: false
        );

        // Register each generated button
        foreach (var kvp in generatedButtons)
        {
            int slotIndex = kvp.Key;
            Button button = kvp.Value;

            _slotButtons[slotIndex] = button;
            slotManager.RegisterSlot(slotIndex, button);

            // Register hover events for tooltip
            button.RegisterCallback<PointerEnterEvent>(_ => OnItemHoverEnter(button));
            button.RegisterCallback<PointerLeaveEvent>(_ => OnItemHoverLeave(button));
        }
    }

    public override bool IsSlotEmpty(int slotIndex)
    {
        return slotManager.IsEmpty(slotIndex);
    }

    /// <summary>
    /// Attempts to start a drag operation from a HUD slot at the given screen position.
    /// Called by: InventoryDragHandler.TickFromInputSystem when detecting drag start from HUD.
    /// Returns slot data and removes it visually (actual SlotManager.TryTake clears the data).
    /// </summary>
    public bool TryTakeFromSlotAtScreenPosition(
        Vector2 uiToolkitScreenPosition,
        out int slotIndex,
        out string itemId,
        out Sprite sprite
    )
    {
        slotIndex = 0;
        itemId = null;
        sprite = null;

        TryInitializeIfNeeded();

        if (root == null || root.panel == null)
            return false;

        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, uiToolkitScreenPosition);
        if (!TryResolveHudSlotButton(panelPos, out slotIndex, out Button slotButton))
            return false;

        if (!slotManager.TryTake(slotIndex, out itemId, out sprite))
            return false;

        ClearSlotVisual(slotButton);
        return true;
    }

    /// <summary>
    /// Restores an item to a HUD slot (used when drag is cancelled or fails).
    /// Called by: InventoryDragHandler when mediator rejects drop or user cancels drag.
    /// </summary>
    public void RestoreSlot(int slotIndex, string itemId, Sprite sprite)
    {
        TryInitializeIfNeeded();

        if (string.IsNullOrEmpty(itemId))
            return;

        if (!_slotButtons.TryGetValue(slotIndex, out Button slotButton) || slotButton == null)
            return;

        AssignSlot(slotIndex, slotButton, itemId, sprite);
    }

    /// <summary>
    /// Gets slot data without removing it (used by IDropTarget for swap validation).
    /// Called by: TryAcceptItem when determining if swap is needed.
    /// </summary>
    public bool TryGetSlotData(int slotIndex, out string itemId, out Sprite sprite)
    {
        itemId = null;
        sprite = null;

        if (!slotManager.TryGet(slotIndex, out var slotData))
            return false;

        itemId = slotData.ItemId;
        sprite = slotData.Sprite;
        return true;
    }

    /// <summary>
    /// Checks if there's a HUD slot at the given screen position (without modifying data).
    /// Called by: External systems that need to query slot presence before operations.
    /// Note: IDropTarget.IsUnderPosition is preferred for drop validation.
    /// </summary>
    public bool TryGetSlotAtScreenPosition(Vector2 uiToolkitScreenPosition, out int slotIndex)
    {
        slotIndex = 0;
        TryInitializeIfNeeded();

        if (root == null || root.panel == null)
            return false;

        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, uiToolkitScreenPosition);
        return TryResolveHudSlotButton(panelPos, out slotIndex, out _);
    }

    /// <summary>
    /// Activates the item in a HUD slot (triggered by keyboard 1-9).
    /// Called by: HandleQuickSlotPressed when corresponding key is pressed.
    /// Fires OnSlotActivated event and equips weapon sprite if applicable.
    /// </summary>
    public void ActivateSlot(int slotIndex)
    {
        if (!slotManager.TryGet(slotIndex, out var slotData))
        {
            logger?.Log($"Slot{slotIndex} activated but empty", this);
            return;
        }

        OnSlotActivated?.Invoke(slotIndex, slotData.ItemId);

        if (spriteLoader != null && !string.IsNullOrEmpty(slotData.ItemId))
        {
            logger?.Log($"Calling EquipWeapon with itemId: {slotData.ItemId}", this);
            try
            {
                spriteLoader.EquipWeapon(slotData.ItemId);
            }
            catch (System.Exception ex)
            {
                logger?.Log(
                    $"Exception calling EquipWeapon: {ex.Message}\n{ex.StackTrace}",
                    this,
                    Logging.LogType.Error
                );
            }
        }
        else if (spriteLoader == null)
        {
            logger?.Log(
                "SpriteLoader not set, cannot equip weapon sprite",
                this,
                Logging.LogType.Warning
            );
        }
        else if (string.IsNullOrEmpty(slotData.ItemId))
        {
            logger?.Log(
                "ItemId is null or empty, cannot equip weapon",
                this,
                Logging.LogType.Warning
            );
        }
    }

    private void AssignSlot(int slotIndex, Button slotButton, string itemId, Sprite sprite)
    {
        slotManager.Assign(slotIndex, itemId, sprite, slotButton);

        if (sprite != null)
        {
            // Keep existing styling (button background frame) but show item as an overlay background.
            try
            {
                slotButton.iconImage = sprite.texture;
            }
            catch
            {
                // Fallback: background image.
                slotButton.style.backgroundImage = new StyleBackground(sprite);
                slotButton.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            }
        }
        else
        {
            slotButton.iconImage = null;
        }

        logger?.Log($"[HUD] Assigned itemId={itemId} to Slot{slotIndex}", this);
    }

    private void ClearSlotVisual(Button slotButton)
    {
        if (slotButton != null)
        {
            slotButton.iconImage = null;
            slotButton.style.backgroundImage = StyleKeyword.Null;
        }
    }

    /// <summary>
    /// Resolves which HUD slot button contains a given panel position.
    /// Used by: drag start detection, drop validation (IDropTarget), and hover checks.
    /// Iterates through all registered buttons and checks if their worldBound contains the position.
    /// </summary>
    private bool TryResolveHudSlotButton(
        Vector2 panelPosition,
        out int slotIndex,
        out Button slotButton
    )
    {
        slotIndex = 0;
        slotButton = null;

        foreach (var kvp in _slotButtons)
        {
            var button = kvp.Value;
            if (button == null)
                continue;

            if (button.worldBound.Contains(panelPosition))
            {
                slotIndex = kvp.Key;
                slotButton = button;
                return true;
            }
        }

        return false;
    }

    public string ContainerName => "HUD QuickSlots";

    public bool TryAcceptItem(
        string itemId,
        Sprite sprite,
        Vector2 screenPosition,
        out ItemPlacementResult result
    )
    {
        result = ItemPlacementResult.Failed();

        if (string.IsNullOrEmpty(itemId))
            return false;

        TryInitializeIfNeeded();

        if (root == null || root.panel == null)
            return false;

        // Find HUD slot at screen position
        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, screenPosition);
        if (!TryResolveHudSlotButton(panelPos, out int slotIndex, out Button slotButton))
            return false;

        // Check if slot is empty or occupied
        if (IsSlotEmpty(slotIndex))
        {
            // Empty slot: assign item
            AssignSlot(slotIndex, slotButton, itemId, sprite);
            result = ItemPlacementResult.PlacedInEmptySlot();
            logger?.Log($"[HUD] Placed {itemId} in quickslot {slotIndex}", this);
            return true;
        }
        else
        {
            // Occupied slot: swap
            if (TryGetSlotData(slotIndex, out string existingItemId, out Sprite existingSprite))
            {
                // Assign new item to slot
                AssignSlot(slotIndex, slotButton, itemId, sprite);

                result = ItemPlacementResult.SwappedWith(
                    existingItemId,
                    existingSprite,
                    slotButton
                );
                logger?.Log(
                    $"[HUD] Swapped {existingItemId} with {itemId} in quickslot {slotIndex}",
                    this
                );
                return true;
            }
        }

        return false;
    }

    public bool IsUnderPosition(Vector2 screenPosition)
    {
        TryInitializeIfNeeded();

        if (root == null || root.panel == null)
            return false;

        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, screenPosition);
        return TryResolveHudSlotButton(panelPos, out _, out _);
    }
}
