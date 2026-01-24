using System.Collections.Generic;
using API;
using FeedTheRealm.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryController : BaseSlotContainer, IDropTarget
{
    [Header("UI References")]
    private List<VisualElement> slots = new List<VisualElement>();
    private bool isUIVisible = false;
    private InventoryDragHandler dragHandler;
    private InventorySlotManager slotUIManager;

    [Header("World Items (sprite-based)")]
    [SerializeField]
    [Tooltip("Service used to download item sprites by spriteId for world-defined items.")]
    private ItemAssetsService itemAssetsService;
    public string ContainerName => "Inventory";

    [Header("Slot Sprites")]
    [SerializeField]
    private Sprite slotNormalSprite;

    [SerializeField]
    private Sprite slotHoverSprite;

    [Header("HUD References")]
    [SerializeField]
    [Tooltip(
        "Registry asset used to locate the HUD fast-use slots controller at runtime (prefab-safe)."
    )]
    private HudFastUseSlotsRegistry hudFastSlotsRegistry;

    private VisualElement dropZone;

    protected override void Awake()
    {
        // Configure slot count for Inventory (naming is fixed: "Slot1"..."Slot12")
        slotCount = 12;
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        HideUI();

        // Initialize slot UI manager and slots
        slotUIManager = new InventorySlotManager(root, slotNormalSprite, slotHoverSprite);
        slots = slotUIManager.InitializeSlots(
            12,
            (evt, slot) => OnSlotPointerDown(evt),
            (evt, slot) => { },
            (evt, slot) => OnItemHoverEnter(slot),
            (evt, slot) => OnItemHoverLeave(slot)
        );

        // Register slots in the SlotManager (1-based to match slot names "Slot1", "Slot2"...)
        for (int i = 0; i < slots.Count; i++)
        {
            slotManager.RegisterSlot(i + 1, slots[i]);
        }

        dropZone = root.Q<VisualElement>("Drop");
        if (dropZone != null)
        {
            dropZone.RegisterCallback<PointerUpEvent>(OnDropZonePointerUp);
        }

        // Initialize drag handler (HUD ref may arrive later via registry)
        if (hudFastSlotsRegistry == null)
        {
            logger?.Log(
                "[InventoryController] HudFastUseSlotsRegistry is not assigned; inventory->HUD drag-drop will be disabled.",
                this,
                Logging.LogType.Warning
            );
        }

        dragHandler = new InventoryDragHandler(
            root,
            slots,
            dropZone,
            logger,
            hudFastSlotsRegistry?.Current
        );
        if (hudFastSlotsRegistry != null)
        {
            hudFastSlotsRegistry.Changed += HandleHudFastSlotsChanged;
        }
        dragHandler.ItemConsumed += HandleItemConsumed;

        // Register drop targets with mediator
        dragHandler.RegisterDropTarget(this);
        if (hudFastSlotsRegistry?.Current != null)
        {
            dragHandler.RegisterDropTarget(hudFastSlotsRegistry.Current);
        }
    }

    private void OnDestroy()
    {
        if (hudFastSlotsRegistry != null)
        {
            hudFastSlotsRegistry.Changed -= HandleHudFastSlotsChanged;
        }
    }

    private void HandleHudFastSlotsChanged(HudFastUseSlotsController controller)
    {
        dragHandler?.SetHudFastSlotsController(controller);

        // Re-register new HUD controller with mediator
        if (controller != null)
        {
            dragHandler?.RegisterDropTarget(controller);
        }
    }

    void Update()
    {
        if (!isUIVisible)
            return;

        // When inventory and HUD are different UI Toolkit panels, pointer events can stop reaching
        // the inventory panel mid-drag. This tick keeps the drag responsive and finalizes drops.
        dragHandler?.TickFromInputSystem(CreateInventoryItemElement);
    }

    private VisualElement CreateInventoryItemElement(string itemId, Sprite sprite)
    {
        return CreateItemElement(itemId, sprite);
    }

    private void OnSlotPointerDown(PointerDownEvent evt)
    {
        itemStatsTooltip?.HideTooltip();
        dragHandler.OnSlotPointerDown(evt);
    }

    private void OnDropZonePointerUp(PointerUpEvent evt)
    {
        dragHandler.OnDropZonePointerUp(evt);
    }

    /// <summary>
    /// Add item to inventory by item ID.
    /// </summary>
    public void AddItemById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            logger.Log("Cannot add item: itemId is null or empty", this, Logging.LogType.Warning);
            return;
        }

        var itemData = Worlds.WorldItemsRegistry.GetItemById(itemId);
        if (itemData == null)
        {
            logger.Log(
                $"Cannot add item: itemId '{itemId}' not found in registry",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        if (itemAssetsService == null)
        {
            logger.Log(
                "Cannot add item: ItemAssetsService reference is missing",
                this,
                Logging.LogType.Error
            );
            return;
        }

        AddItemBySpriteWithId(itemId, itemId);
    }

    /// <summary>
    /// Add item to inventory by spriteId with itemId (enables tooltip functionality).
    /// Downloads the sprite (with cache in ItemAssetsService) and adds it to the first empty slot.
    /// </summary>
    private async void AddItemBySpriteWithId(string spriteId, string itemId)
    {
        if (string.IsNullOrEmpty(spriteId))
        {
            logger.Log("Cannot add item: spriteId is null or empty", this, Logging.LogType.Warning);
            return;
        }
        if (itemAssetsService == null)
        {
            logger.Log(
                "Cannot add item: ItemAssetsService reference is missing",
                this,
                Logging.LogType.Error
            );
            return;
        }
        await AddItemBySpriteWithIdAsync(spriteId, itemId);
    }

    private async System.Threading.Tasks.Task AddItemBySpriteWithIdAsync(
        string spriteId,
        string itemId
    )
    {
        Texture2D texture = await itemAssetsService.DownloadItemSpriteAsync(spriteId);

        if (texture != null)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            foreach (var slot in slots)
            {
                if (slot.childCount == 0)
                {
                    CreateItemElement(sprite, slot, itemId);
                    logger.Log($"Item added to inventory with ID: {itemId}", this);
                    return;
                }
            }

            logger.Log("Inventory full, cannot add more items", this, Logging.LogType.Warning);
        }
        else
        {
            logger.Log(
                $"Failed to load item sprite for spriteId: {spriteId}",
                this,
                Logging.LogType.Error
            );
        }
    }

    private void CreateItemElement(Sprite itemSprite, VisualElement parentSlot, string itemId)
    {
        var itemElement = CreateItemElement(itemId, itemSprite);
        if (itemElement != null)
        {
            parentSlot.Add(itemElement);
            logger.Log(
                $"Item created in slot: {parentSlot.name}"
                    + (itemId != null ? $" (ID: {itemId})" : ""),
                this
            );
        }
    }

    private void HandleItemConsumed(VisualElement itemElement)
    {
        if (itemElement == null)
            return;

        UntrackItemElement(itemElement);
        itemStatsTooltip?.HideTooltip();
    }

    public bool IsInventoryFull()
    {
        foreach (var slot in slots)
        {
            if (slot.childCount == 0)
            {
                return false;
            }
        }
        return true;
    }

    public int GetEmptySlotCount()
    {
        logger?.Log(
            $"[InventoryController] Counting empty slots. Total slots in list: {slots.Count}",
            this
        );

        int count = 0;
        foreach (var slot in slots)
        {
            if (slot.childCount == 0)
            {
                count++;
            }
        }

        logger?.Log($"[InventoryController] Empty slots found: {count}/{slots.Count}", this);
        return count;
    }

    public bool IsOpen()
    {
        return isUIVisible;
    }

    public void ToggleInventory()
    {
        //logger.Log("Toggle inventory", this);

        if (isUIVisible)
        {
            HideUI();
        }
        else
        {
            ShowUI();
        }
    }

    private void ShowUI()
    {
        if (root != null)
        {
            root.style.display = DisplayStyle.Flex;
            isUIVisible = true;

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            //logger.Log("Inventory UI shown - Cursor unlocked", this);
        }
    }

    private void HideUI()
    {
        if (root != null)
        {
            root.style.display = DisplayStyle.None;
            isUIVisible = false;

            if (itemStatsTooltip != null)
            {
                itemStatsTooltip.HideTooltip();
            }

            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            //logger.Log("Inventory UI hidden - Cursor locked", this);
        }
    }

    public bool TryAcceptItem(
        string itemId,
        Sprite sprite,
        Vector2 screenPosition,
        out ItemPlacementResult result
    )
    {
        result = ItemPlacementResult.Failed();

        if (string.IsNullOrEmpty(itemId) || sprite == null)
            return false;

        // Check if screen position is over any inventory slot
        if (root == null || root.panel == null)
            return false;

        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, screenPosition);
        VisualElement targetSlot = FindSlotAtPosition(panelPos);

        if (targetSlot == null)
            return false;

        // Get slot index
        if (!TryGetSlotIndexFromElement(targetSlot, out int slotIndex))
            return false;

        // Check if slot is empty or occupied
        if (targetSlot.childCount == 0)
        {
            // Empty slot: place item
            var itemElement = CreateInventoryItemElement(itemId, sprite);
            if (itemElement != null)
            {
                itemElement.RemoveFromHierarchy();
                targetSlot.Add(itemElement);
                InventoryItemFactory.ResetItemStyles(itemElement);
                slotManager.Assign(slotIndex, itemId, sprite, targetSlot);
                result = ItemPlacementResult.PlacedInEmptySlot();
                logger?.Log($"[Inventory] Placed {itemId} in slot {slotIndex}", this);
                return true;
            }
        }
        else
        {
            // Occupied slot: swap
            var existingItem = targetSlot[0];
            if (
                InventoryItemFactory.TryGetItemData(
                    existingItem,
                    out string existingItemId,
                    out Sprite existingSprite
                )
            )
            {
                // Remove existing item
                existingItem.RemoveFromHierarchy();
                UntrackItemElement(existingItem);

                // Place new item
                var newItemElement = CreateInventoryItemElement(itemId, sprite);
                if (newItemElement != null)
                {
                    newItemElement.RemoveFromHierarchy();
                    targetSlot.Add(newItemElement);
                    InventoryItemFactory.ResetItemStyles(newItemElement);
                    slotManager.Assign(slotIndex, itemId, sprite, targetSlot);

                    result = ItemPlacementResult.SwappedWith(
                        existingItemId,
                        existingSprite,
                        targetSlot
                    );
                    logger?.Log(
                        $"[Inventory] Swapped {existingItemId} with {itemId} in slot {slotIndex}",
                        this
                    );
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsUnderPosition(Vector2 screenPosition)
    {
        if (root == null || root.panel == null || !isUIVisible)
            return false;

        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, screenPosition);
        return FindSlotAtPosition(panelPos) != null;
    }

    private VisualElement FindSlotAtPosition(Vector2 panelPosition)
    {
        if (slots == null)
            return null;

        foreach (var slot in slots)
        {
            if (slot != null && slot.worldBound.Contains(panelPosition))
                return slot;
        }

        return null;
    }
}
