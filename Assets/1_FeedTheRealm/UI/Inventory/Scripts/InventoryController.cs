using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class InventoryController : MonoBehaviour {
    [Header("UI References")]
    private UIDocument uiDocument;
    private VisualElement root;
    private List<VisualElement> slots = new List<VisualElement>();
    private VisualElement draggedItem;
    private VisualElement draggedItemOriginalSlot;
    private Vector2 dragOffset;
    private bool isUIVisible = false;

    [Header("Items Management")]
    // ItemsManager reference (uses singleton pattern)
    private Items.ItemsManager ItemsManager => Items.ItemsManager.Instance;

    [Header("Debug - Loot Testing")]
    [SerializeField] private bool enableDebugLootButton = false;
    [SerializeField] private List<Sprite> debugLootSprites = new List<Sprite>();
    private int currentLootIndex = 0;

    [Header("Slot Sprites")]
    [SerializeField] private Sprite slotNormalSprite;
    [SerializeField] private Sprite slotHoverSprite;

    [Header("Logging")]
    [SerializeField] private Logging.Logger logger;

    private Button lootButton;
    private VisualElement dropZone;

    void Start() {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Initially hide the UI (but keep the GameObject active)
        HideUI();

        // Find all slots
        for (int i = 1; i <= 12; i++) {
            var slot = root.Q<VisualElement>($"Slot{i}");
            if (slot != null) {
                slots.Add(slot);
                slot.RegisterCallback<PointerDownEvent>(OnSlotPointerDown);
                slot.RegisterCallback<PointerUpEvent>(OnSlotPointerUp);

                // Configure hover sprites if assigned
                if (slotNormalSprite != null && slotHoverSprite != null) {
                    slot.RegisterCallback<PointerEnterEvent>(evt => OnSlotHoverEnter(evt, slot));
                    slot.RegisterCallback<PointerLeaveEvent>(evt => OnSlotHoverLeave(evt, slot));
                }
            }
        }

        // Configure Loot button (debug only)
        lootButton = root.Q<Button>("Loot");
        if (lootButton != null && enableDebugLootButton) {
            lootButton.clicked += OnLootButtonClicked;
        } else if (lootButton != null) {
            // Hide the button if not in debug mode
            lootButton.style.display = DisplayStyle.None;
        }

        // Configure Drop zone
        dropZone = root.Q<VisualElement>("Drop");
        if (dropZone != null) {
            dropZone.RegisterCallback<PointerUpEvent>(OnDropZonePointerUp);
        }

        // Register global events for drag
        // PointerMove with TrickleDown to capture across the entire screen
        root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);

        // PointerUp WITHOUT TrickleDown so it executes AFTER OnSlotPointerUp
        root.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);

        // Also register on panel to ensure complete coverage
        var panel = root.panel;
        if (panel != null) {
            panel.visualTree.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            // PointerUp on panel also without TrickleDown
            panel.visualTree.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);
        }
    }

    private void OnSlotPointerDown(PointerDownEvent evt) {
        logger.Log($"OnSlotPointerDown - Target: {evt.target}, CurrentTarget: {evt.currentTarget}", this);

        // Try to get the slot from currentTarget (the element that registered the callback)
        var slot = evt.currentTarget as VisualElement;

        if (slot != null && slot.childCount > 0) {
            logger.Log($"Slot found with {slot.childCount} child(ren)", this);
            draggedItem = slot[0]; // Assume the first child is the item
            draggedItemOriginalSlot = slot; // Save the original slot

            // Get item position before removing it
            Rect itemWorldBound = draggedItem.worldBound;

            // Remove from slot and add to root so it's visible over everything
            draggedItem.RemoveFromHierarchy();
            root.Add(draggedItem);
            draggedItem.BringToFront();

            // Calculate offset using the item's world position
            dragOffset = new Vector2(evt.position.x - itemWorldBound.x, evt.position.y - itemWorldBound.y);

            // Convert world position to root local
            Vector2 pointerInRoot = root.WorldToLocal(evt.position);

            // Position immediately at the correct position
            draggedItem.style.position = Position.Absolute;
            draggedItem.style.left = pointerInRoot.x - dragOffset.x;
            draggedItem.style.top = pointerInRoot.y - dragOffset.y;

            logger.Log($"Starting drag - Item: {draggedItem.name}, Offset: {dragOffset}, ItemWorldPos: ({itemWorldBound.x}, {itemWorldBound.y})", this);
            evt.StopPropagation();
        } else {
            logger.Log($"Could not start drag - Slot null: {slot == null}, ChildCount: {slot?.childCount ?? 0}", this, Logging.LogType.Warning);
        }
    }

    private void OnPointerMove(PointerMoveEvent evt) {
        if (draggedItem != null) {
            // Convert global pointer position to root local coordinates
            Vector2 pointerInRoot = root.WorldToLocal(evt.position);

            // Apply offset
            draggedItem.style.left = pointerInRoot.x - dragOffset.x;
            draggedItem.style.top = pointerInRoot.y - dragOffset.y;
            draggedItem.style.position = Position.Absolute;

            // Prevent event propagation
            evt.StopPropagation();
            // Debug.Log($"Dragging - PointerInRoot: {pointerInRoot}, ItemPos: ({draggedItem.style.left.value.value}, {draggedItem.style.top.value.value})");
        }
    }

    private void OnSlotPointerUp(PointerUpEvent evt) {
        logger.Log($"OnSlotPointerUp - Target: {evt.target}, DraggedItem: {draggedItem != null}", this);

        if (draggedItem != null) {
            var targetSlot = evt.currentTarget as VisualElement;
            logger.Log($"Target slot: {targetSlot?.name}, Is in slots list: {slots.Contains(targetSlot)}", this);

            if (targetSlot != null && slots.Contains(targetSlot)) {
                // Move/swap the item to the destination slot
                logger.Log($"Moving/swapping item to slot: {targetSlot.name}", this);
                MoveItemToSlot(draggedItem, targetSlot);

                // Clear references AFTER moving
                draggedItem = null;
                draggedItemOriginalSlot = null;

                // Important: stop propagation immediately
                evt.StopImmediatePropagation();
            }
        }
    }

    private void OnGlobalPointerUp(PointerUpEvent evt) {
        logger.Log($"OnGlobalPointerUp - DraggedItem: {draggedItem != null}, Position: {evt.position}", this);

        // Only process if there's still an item being dragged
        // (if OnSlotPointerUp handled it, draggedItem will be null)
        if (draggedItem != null) {
            // Check if released over the drop zone
            bool isOverDrop = IsPointerOverElement(evt.position, dropZone);
            logger.Log($"Is over drop zone: {isOverDrop}, DropZone null: {dropZone == null}", this);

            if (isOverDrop) {
                // Delete the item
                draggedItem.RemoveFromHierarchy();
                logger.Log("Item deleted in Drop zone (Global)", this);
            } else {
                // If released outside a slot, return it
                logger.Log("Returning item (from global)", this);
                ReturnItemToOriginalSlot();
            }

            draggedItem = null;
            draggedItemOriginalSlot = null;
        }
    }

    private void OnDropZonePointerUp(PointerUpEvent evt) {
        if (draggedItem != null) {
            // Delete the dragged item
            draggedItem.RemoveFromHierarchy();
            logger.Log("Item deleted in Drop zone", this);
            draggedItem = null;
            draggedItemOriginalSlot = null;
            evt.StopPropagation();
        }
    }

    private void OnSlotHoverEnter(PointerEnterEvent evt, VisualElement slot) {
        if (slotHoverSprite != null) {
            slot.style.backgroundImage = new StyleBackground(slotHoverSprite);
        }
    }

    private void OnSlotHoverLeave(PointerLeaveEvent evt, VisualElement slot) {
        if (slotNormalSprite != null) {
            slot.style.backgroundImage = new StyleBackground(slotNormalSprite);
        }
    }

    private void MoveItemToSlot(VisualElement item, VisualElement targetSlot) {
        // If the destination slot has an item AND it's not the original slot, swap
        if (targetSlot.childCount > 0 && targetSlot != draggedItemOriginalSlot) {
            logger.Log("Slot occupied, swapping items", this);
            var targetItem = targetSlot[0];

            // Remove both items
            item.RemoveFromHierarchy();
            targetItem.RemoveFromHierarchy();

            // Swap positions
            targetSlot.Add(item);
            if (draggedItemOriginalSlot != null) {
                draggedItemOriginalSlot.Add(targetItem);
            }

            // Reset styles for both items
            item.style.position = Position.Relative;
            item.style.left = 0;
            item.style.top = 0;

            targetItem.style.position = Position.Relative;
            targetItem.style.left = 0;
            targetItem.style.top = 0;
        } else {
            // Empty slot or same original slot, just move
            item.RemoveFromHierarchy();
            targetSlot.Add(item);

            // Reset position
            item.style.position = Position.Relative;
            item.style.left = 0;
            item.style.top = 0;
        }
    }

    private void ReturnItemToOriginalSlot() {
        // Try to return to original slot if we have it saved
        if (draggedItemOriginalSlot != null) {
            draggedItemOriginalSlot.Add(draggedItem);
            draggedItem.style.position = Position.Relative;
            draggedItem.style.left = 0;
            draggedItem.style.top = 0;
            return;
        }

        // If not, find an empty slot
        foreach (var slot in slots) {
            if (slot.childCount == 0) {
                slot.Add(draggedItem);
                draggedItem.style.position = Position.Relative;
                draggedItem.style.left = 0;
                draggedItem.style.top = 0;
                return;
            }
        }

        // If there are no empty slots, add to the first one
        if (slots.Count > 0) {
            slots[0].Add(draggedItem);
            draggedItem.style.position = Position.Relative;
            draggedItem.style.left = 0;
            draggedItem.style.top = 0;
        }
    }

    private bool IsPointerOverElement(Vector2 pointerPosition, VisualElement element) {
        if (element == null) return false;

        var rect = element.worldBound;
        return rect.Contains(pointerPosition);
    }

    /// <summary>
    /// DEBUG ONLY: Button to simulate loot drop with predefined sprites
    /// </summary>
    private void OnLootButtonClicked() {
        if (debugLootSprites == null || debugLootSprites.Count == 0) {
            logger.Log("[DEBUG] No sprites assigned in debugLootSprites", this, Logging.LogType.Warning);
            return;
        }

        // Get the current sprite from the list
        Sprite spriteToAdd = debugLootSprites[currentLootIndex];

        // Advance to the next index (with wrap-around)
        currentLootIndex = (currentLootIndex + 1) % debugLootSprites.Count;

        AddItemBySprite(spriteToAdd);
        logger.Log($"[DEBUG] Looted sprite {currentLootIndex}/{debugLootSprites.Count}", this);
    }

    /// <summary>
    /// Add item to inventory by item ID (gets sprite from ItemsManager).
    /// This is the main method used by the game.
    /// </summary>
    public void AddItemById(string itemId) {
        if (string.IsNullOrEmpty(itemId)) {
            logger.Log("Cannot add item: itemId is null or empty", this, Logging.LogType.Warning);
            return;
        }

        if (ItemsManager == null) {
            logger.Log("ERROR: ItemsManager singleton not available! Make sure ItemsManager exists in MPMenuScene.", this, Logging.LogType.Error);
            return;
        }

        if (!ItemsManager.IsInitialized) {
            logger.Log("WARNING: ItemsManager not initialized yet, cannot add item", this, Logging.LogType.Warning);
            return;
        }

        // Get sprite from ItemsManager (coroutine for async loading)
        StartCoroutine(AddItemByIdCoroutine(itemId));
    }

    private System.Collections.IEnumerator AddItemByIdCoroutine(string itemId) {
        Texture2D texture = null;

        yield return ItemsManager.GetItemSprite(itemId, (loadedTexture) => {
            texture = loadedTexture;
        });

        if (texture != null) {
            // Convert Texture2D to Sprite
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            AddItemBySprite(sprite);
            logger.Log($"Item added to inventory: {itemId}", this);
        } else {
            logger.Log($"Failed to load sprite for item: {itemId}", this, Logging.LogType.Error);
        }
    }

    /// <summary>
    /// Add item to inventory by sprite directly (used by debug button).
    /// </summary>
    public void AddItemBySprite(Sprite itemSprite) {
        if (itemSprite == null) {
            logger.Log("Cannot add item: sprite is null", this, Logging.LogType.Warning);
            return;
        }

        // Find the first empty slot
        foreach (var slot in slots) {
            if (slot.childCount == 0) {
                CreateItemElement(itemSprite, slot);
                logger.Log($"Item added to inventory", this);
                return;
            }
        }

        logger.Log("Inventory full, cannot add more items", this, Logging.LogType.Warning);
    }

    private void CreateItemElement(Sprite itemSprite, VisualElement parentSlot) {
        var itemElement = new VisualElement();
        itemElement.name = "InventoryItem";
        itemElement.style.backgroundImage = new StyleBackground(itemSprite);
        itemElement.style.width = 45;
        itemElement.style.height = 45;
        itemElement.style.position = Position.Relative;
        itemElement.AddToClassList("inventory-item");

        // Important: allow events to pass to parent slot
        itemElement.pickingMode = PickingMode.Ignore;

        parentSlot.Add(itemElement);
        logger.Log($"Item created in slot: {parentSlot.name}", this);
    }

    public bool IsInventoryFull() {
        foreach (var slot in slots) {
            if (slot.childCount == 0) {
                return false;
            }
        }
        return true;
    }

    public int GetEmptySlotCount() {
        logger?.Log($"[InventoryController] Counting empty slots. Total slots in list: {slots.Count}", this);

        int count = 0;
        foreach (var slot in slots) {
            if (slot.childCount == 0) {
                count++;
            }
        }

        logger?.Log($"[InventoryController] Empty slots found: {count}/{slots.Count}", this);
        return count;
    }

    public bool IsOpen() {
        return isUIVisible;
    }

    public void ToggleInventory() {
        //logger.Log("Toggle inventory", this);

        if (isUIVisible) {
            HideUI();
        } else {
            ShowUI();
        }
    }

    private void ShowUI() {
        if (root != null) {
            root.style.display = DisplayStyle.Flex;
            isUIVisible = true;

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            //logger.Log("Inventory UI shown - Cursor unlocked", this);
        }
    }

    private void HideUI() {
        if (root != null) {
            root.style.display = DisplayStyle.None;
            isUIVisible = false;

            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            //logger.Log("Inventory UI hidden - Cursor locked", this);
        }
    }
}
