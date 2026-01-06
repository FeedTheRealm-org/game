using System.Collections.Generic;
using API;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryController : MonoBehaviour
{
    [Header("UI References")]
    private UIDocument uiDocument;
    private VisualElement root;
    private List<VisualElement> slots = new List<VisualElement>();
    private bool isUIVisible = false;

    // Track itemId for each item element (used for tooltip)
    private Dictionary<VisualElement, string> itemIdMap = new Dictionary<VisualElement, string>();

    // Helpers
    private InventoryDragHandler dragHandler;
    private InventorySlotManager slotManager;

    [Header("World Items (sprite-based)")]
    [SerializeField]
    [Tooltip("Service used to download item sprites by spriteId for world-defined items.")]
    private ItemAssetsService itemAssetsService;

    [Header("Tooltip")]
    [SerializeField]
    private ItemStatsTooltip itemStatsTooltip;
    private int currentLootIndex = 0;

    [Header("Slot Sprites")]
    [SerializeField]
    private Sprite slotNormalSprite;

    [SerializeField]
    private Sprite slotHoverSprite;

    [Header("Logging")]
    [SerializeField]
    private Logging.Logger logger;

    private VisualElement dropZone;

    void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        HideUI();

        // Initialize slot manager and slots
        slotManager = new InventorySlotManager(root, slotNormalSprite, slotHoverSprite);
        slots = slotManager.InitializeSlots(
            12,
            (evt, slot) => OnSlotPointerDown(evt),
            (evt, slot) => OnSlotPointerUp(evt),
            (evt, slot) => OnItemHoverEnter(evt, slot),
            (evt, slot) => OnItemHoverLeave(evt, slot)
        );

        dropZone = root.Q<VisualElement>("Drop");
        if (dropZone != null)
        {
            dropZone.RegisterCallback<PointerUpEvent>(OnDropZonePointerUp);
        }

        // Initialize drag handler
        dragHandler = new InventoryDragHandler(root, slots, dropZone, logger);

        root.RegisterCallback<PointerMoveEvent>(
            evt => dragHandler.OnPointerMove(evt),
            TrickleDown.TrickleDown
        );
        root.RegisterCallback<PointerUpEvent>(evt =>
            dragHandler.OnGlobalPointerUp(evt, ReturnItemToOriginalSlot)
        );

        var panel = root.panel;
        if (panel != null)
        {
            panel.visualTree.RegisterCallback<PointerMoveEvent>(
                evt => dragHandler.OnPointerMove(evt),
                TrickleDown.TrickleDown
            );
            panel.visualTree.RegisterCallback<PointerUpEvent>(evt =>
                dragHandler.OnGlobalPointerUp(evt, ReturnItemToOriginalSlot)
            );
        }
    }

    private void OnSlotPointerDown(PointerDownEvent evt)
    {
        // Hide tooltip when starting drag
        itemStatsTooltip?.HideTooltip();
        dragHandler.OnSlotPointerDown(evt);
    }

    // Drag logic is now handled by InventoryDragHandler

    private void OnSlotPointerUp(PointerUpEvent evt)
    {
        dragHandler.OnSlotPointerUp(evt, MoveItemToSlot);
    }

    // Global pointer up handled by InventoryDragHandler

    private void OnDropZonePointerUp(PointerUpEvent evt)
    {
        dragHandler.OnDropZonePointerUp(evt);
    }

    private void OnSlotHoverEnter(PointerEnterEvent evt, VisualElement slot)
    {
        if (slotHoverSprite != null)
        {
            slot.style.backgroundImage = new StyleBackground(slotHoverSprite);
        }
    }

    private void OnSlotHoverLeave(PointerLeaveEvent evt, VisualElement slot)
    {
        if (slotNormalSprite != null)
        {
            slot.style.backgroundImage = new StyleBackground(slotNormalSprite);
        }
    }

    private void MoveItemToSlot(VisualElement item, VisualElement targetSlot)
    {
        // Use dragHandler's reference for original slot
        var originalSlot = dragHandler.DraggedItemOriginalSlot;
        if (targetSlot.childCount > 0 && targetSlot != originalSlot)
        {
            logger.Log("Slot occupied, swapping items", this);
            var targetItem = targetSlot[0];
            item.RemoveFromHierarchy();
            targetItem.RemoveFromHierarchy();
            targetSlot.Add(item);
            if (originalSlot != null)
            {
                originalSlot.Add(targetItem);
            }
            ResetItemStyles(item);
            ResetItemStyles(targetItem);
        }
        else
        {
            item.RemoveFromHierarchy();
            targetSlot.Add(item);
            ResetItemStyles(item);
        }
    }

    /// <summary>
    /// Reset item styles to fill the slot (100% width/height, relative position).
    /// </summary>
    private void ResetItemStyles(VisualElement item)
    {
        // Reset item style to fill the slot
        item.style.position = Position.Relative;
        item.style.left = 0;
        item.style.top = 0;
        item.style.width = new Length(100, LengthUnit.Percent);
        item.style.height = new Length(100, LengthUnit.Percent);
    }

    private void ReturnItemToOriginalSlot()
    {
        var draggedItem = dragHandler.DraggedItem;
        var draggedItemOriginalSlot = dragHandler.DraggedItemOriginalSlot;
        if (draggedItem == null)
            return;
        if (draggedItemOriginalSlot != null)
        {
            draggedItemOriginalSlot.Add(draggedItem);
            ResetItemStyles(draggedItem);
            return;
        }
        foreach (var slot in slots)
        {
            if (slot.childCount == 0)
            {
                slot.Add(draggedItem);
                ResetItemStyles(draggedItem);
                return;
            }
        }
        if (slots.Count > 0)
        {
            slots[0].Add(draggedItem);
            ResetItemStyles(draggedItem);
        }
    }

    // Pointer over element logic is now in InventoryDragHandler

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

        // Lookup item data by id
        var consumable = Worlds.WorldItemsRegistry.GetConsumableById(itemId);
        if (consumable == null)
        {
            logger.Log(
                $"Cannot add item: itemId '{itemId}' not found in registry",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        // Download sprite and add to inventory
        AddItemBySpriteWithId(consumable.spriteId, itemId);
    }

    /// <summary>
    /// Add item to inventory by sprite directly (used by debug button).
    /// Note: This version doesn't have itemId, so tooltip won't work for these items.
    /// </summary>
    public void AddItemBySprite(Sprite itemSprite)
    {
        if (itemSprite == null)
        {
            logger.Log("Cannot add item: sprite is null", this, Logging.LogType.Warning);
            return;
        }

        foreach (var slot in slots)
        {
            if (slot.childCount == 0)
            {
                CreateItemElement(itemSprite, slot, null);
                logger.Log($"Item added to inventory", this);
                return;
            }
        }

        logger.Log("Inventory full, cannot add more items", this, Logging.LogType.Warning);
    }

    /// <summary>
    /// Add item to inventory by spriteId with itemId (enables tooltip functionality).
    /// Downloads the sprite and adds the item to the first empty slot.
    /// </summary>
    private void AddItemBySpriteWithId(string spriteId, string itemId)
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
        StartCoroutine(AddItemBySpriteWithIdCoroutine(spriteId, itemId));
    }

    private System.Collections.IEnumerator AddItemBySpriteWithIdCoroutine(
        string spriteId,
        string itemId
    )
    {
        Texture2D texture = null;
        bool completed = false;

        yield return itemAssetsService.DownloadItemSprite(
            spriteId,
            (loadedTexture) =>
            {
                texture = loadedTexture;
                completed = true;
            }
        );

        yield return new WaitUntil(() => completed);

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
                    yield break;
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
        var itemElement = InventoryItemFactory.CreateItemElement(itemSprite, itemId);
        if (!string.IsNullOrEmpty(itemId))
        {
            itemIdMap[itemElement] = itemId;
        }
        parentSlot.Add(itemElement);
        logger.Log(
            $"Item created in slot: {parentSlot.name}" + (itemId != null ? $" (ID: {itemId})" : ""),
            this
        );
    }

    /// <summary>
    /// Handle hover enter event on item slots to show tooltip.
    /// </summary>
    private void OnItemHoverEnter(PointerEnterEvent evt, VisualElement slot)
    {
        logger?.Log(
            $"[Tooltip] OnItemHoverEnter - Slot: {slot.name}, ChildCount: {slot.childCount}",
            this
        );

        if (slot.childCount == 0)
        {
            logger?.Log("[Tooltip] Slot is empty, skipping", this);
            return;
        }

        if (dragHandler != null && dragHandler.DraggedItem != null)
        {
            logger?.Log("[Tooltip] Currently dragging, skipping", this);
            return;
        }

        var itemElement = slot[0];
        logger?.Log($"[Tooltip] Item element found: {itemElement.name}", this);

        if (itemIdMap.TryGetValue(itemElement, out string itemId))
        {
            logger?.Log($"[Tooltip] ItemId found in map: {itemId}", this);
            if (itemStatsTooltip != null)
            {
                itemStatsTooltip.ShowTooltip(itemId, slot);
                logger?.Log($"[Tooltip] Showing tooltip for item: {itemId}", this);
            }
            else
            {
                logger?.Log("[Tooltip] itemStatsTooltip is null!", this, Logging.LogType.Warning);
            }
        }
        else
        {
            logger?.Log(
                $"[Tooltip] ItemId not found in map for element: {itemElement.name}",
                this,
                Logging.LogType.Warning
            );
        }
    }

    /// <summary>
    /// Handle hover leave event on item slots to hide tooltip.
    /// </summary>
    private void OnItemHoverLeave(PointerLeaveEvent evt, VisualElement slot)
    {
        if (itemStatsTooltip != null)
        {
            itemStatsTooltip.HideTooltip();
            logger?.Log("Hiding tooltip", this);
        }
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
}
