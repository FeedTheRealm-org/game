using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Handles drag and drop logic for inventory items.
/// </summary>
public class InventoryDragHandler
{
    private VisualElement root;
    private List<VisualElement> slots;
    private VisualElement dropZone;
    private Logging.Logger logger;
    private HudFastUseSlotsController hudFastSlots;
    private VisualElement draggedItem;
    private VisualElement draggedItemOriginalSlot;
    private Vector2 dragOffset;

    private DragDropMediator mediator;

    private bool _draggingFromHud;
    private int _hudSourceSlotIndex;
    private string _hudItemId;
    private Sprite _hudSprite;

    public event System.Action<VisualElement> ItemConsumed;

    public InventoryDragHandler(
        VisualElement root,
        List<VisualElement> slots,
        VisualElement dropZone,
        Logging.Logger logger,
        HudFastUseSlotsController hudFastSlots
    )
    {
        this.root = root;
        this.slots = slots;
        this.dropZone = dropZone;
        this.logger = logger;
        this.hudFastSlots = hudFastSlots;

        // Initialize mediator
        mediator = new DragDropMediator(dropZone, logger);
    }

    public void SetHudFastSlotsController(HudFastUseSlotsController controller)
    {
        hudFastSlots = controller;
    }

    /// <summary>
    /// Registers a drop target (Inventory, HUD, etc.) with the mediator.
    /// </summary>
    public void RegisterDropTarget(IDropTarget target)
    {
        mediator?.RegisterDropTarget(target);
    }

    public VisualElement DraggedItem => draggedItem;
    public VisualElement DraggedItemOriginalSlot => draggedItemOriginalSlot;
    public Vector2 DragOffset => dragOffset;

    public void OnSlotPointerDown(PointerDownEvent evt)
    {
        logger.Log(
            $"[DragHandler] OnSlotPointerDown - Target: {evt.target}, CurrentTarget: {evt.currentTarget}",
            null
        );
        var slot = evt.currentTarget as VisualElement;
        if (slot != null && slot.childCount > 0)
        {
            draggedItem = slot[0];
            draggedItemOriginalSlot = slot;
            Rect itemWorldBound = draggedItem.worldBound;
            float itemWidth = itemWorldBound.width;
            float itemHeight = itemWorldBound.height;
            dragOffset = new Vector2(
                evt.position.x - itemWorldBound.x,
                evt.position.y - itemWorldBound.y
            );
            draggedItem.RemoveFromHierarchy();
            root.Add(draggedItem);
            draggedItem.BringToFront();
            Vector2 pointerInRoot = root.WorldToLocal(evt.position);
            InventoryItemFactory.SetAbsolutePosition(
                draggedItem,
                pointerInRoot.x - dragOffset.x,
                pointerInRoot.y - dragOffset.y,
                itemWidth,
                itemHeight
            );
            logger?.Log(
                $"[DragHandler] Starting drag - Item: {draggedItem.name}, Size: ({itemWidth}x{itemHeight}), Offset: {dragOffset}",
                null
            );
            evt.StopPropagation();
        }
    }

    public void OnPointerMove(PointerMoveEvent evt)
    {
        if (draggedItem != null)
        {
            Vector2 pointerInRoot = root.WorldToLocal(evt.position);
            draggedItem.style.left = pointerInRoot.x - dragOffset.x;
            draggedItem.style.top = pointerInRoot.y - dragOffset.y;
            evt.StopPropagation();
        }
    }

    /// <summary>
    /// Fallback drag update path driven by the Input System (screen-space).
    /// This is needed when inventory and HUD are different UI Toolkit panels,
    /// because PointerMove/PointerUp events may stop reaching the inventory panel.
    /// </summary>
    public void TickFromInputSystem(
        System.Func<string, Sprite, VisualElement> createInventoryItemElement
    )
    {
        if (root == null)
            return;

        if (Mouse.current == null)
            return;

        if (root.panel == null)
            return;

        // Input System mouse position is bottom-left origin; UI Toolkit ScreenToPanel expects top-left origin.
        Vector2 rawScreenPos = Mouse.current.position.ReadValue();
        Vector2 uiToolkitScreenPos = new Vector2(rawScreenPos.x, Screen.height - rawScreenPos.y);
        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, uiToolkitScreenPos);

        // Start drag from HUD quickslot (if any) when nothing is being dragged.
        if (draggedItem == null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (
                hudFastSlots != null
                && hudFastSlots.TryTakeFromSlotAtScreenPosition(
                    uiToolkitScreenPos,
                    out int slotIndex,
                    out string itemId,
                    out Sprite sprite
                )
            )
            {
                _draggingFromHud = true;
                _hudSourceSlotIndex = slotIndex;
                _hudItemId = itemId;
                _hudSprite = sprite;

                draggedItem = InventoryItemFactory.CreateDragGhost(itemId, sprite, 48f);
                dragOffset = new Vector2(24f, 24f);
                draggedItemOriginalSlot = null;

                root.Add(draggedItem);
                draggedItem.BringToFront();
            }
        }

        if (draggedItem == null)
            return;

        // Keep following the pointer even when pointer events are dispatched to a different panel.
        Vector2 pointerInRoot = root.WorldToLocal(panelPos);
        draggedItem.style.left = pointerInRoot.x - dragOffset.x;
        draggedItem.style.top = pointerInRoot.y - dragOffset.y;

        if (!Mouse.current.leftButton.wasReleasedThisFrame)
            return;

        // Build drag context
        var context = new DragDropContext
        {
            DraggedElement = draggedItem,
            OriginalSlot = draggedItemOriginalSlot,
            ItemId = _draggingFromHud ? _hudItemId : null,
            ItemSprite = _draggingFromHud ? _hudSprite : null,
            ScreenPosition = uiToolkitScreenPos,
            PanelPosition = panelPos,
            DraggingFromHud = _draggingFromHud,
            HudSourceSlotIndex = _hudSourceSlotIndex,
        };

        // Extract item data if dragging from inventory
        if (!_draggingFromHud && draggedItem != null)
        {
            InventoryItemFactory.TryGetItemData(
                draggedItem,
                out context.ItemId,
                out context.ItemSprite
            );
        }

        // Try to drop item using mediator
        if (mediator != null && mediator.TryDropItem(context, out var result))
        {
            // Item was accepted by a target
            draggedItem.RemoveFromHierarchy();

            if (result.Swapped)
            {
                // Handle swap: return swapped item to original slot
                if (_draggingFromHud)
                {
                    // Swapped from HUD: return inventory item to HUD
                    hudFastSlots?.RestoreSlot(
                        _hudSourceSlotIndex,
                        result.SwappedItemId,
                        result.SwappedSprite
                    );
                }
                else if (draggedItemOriginalSlot != null)
                {
                    // Swapped from inventory: return item to inventory slot
                    var swappedElement = createInventoryItemElement?.Invoke(
                        result.SwappedItemId,
                        result.SwappedSprite
                    );
                    if (swappedElement != null)
                    {
                        swappedElement.RemoveFromHierarchy();
                        draggedItemOriginalSlot.Add(swappedElement);
                        InventoryItemFactory.ResetItemStyles(swappedElement);
                    }
                }

                logger?.Log($"[DragHandler] Swapped items successfully", null);
            }
        }
        else
        {
            // No target accepted: return to original position
            draggedItem.RemoveFromHierarchy();

            if (_draggingFromHud)
            {
                // Restore to HUD
                hudFastSlots?.RestoreSlot(_hudSourceSlotIndex, _hudItemId, _hudSprite);
            }
            else if (draggedItemOriginalSlot != null)
            {
                // Return to original inventory slot
                draggedItemOriginalSlot.Add(draggedItem);
                InventoryItemFactory.ResetItemStyles(draggedItem);
            }
        }

        // Reset drag state
        _draggingFromHud = false;
        _hudSourceSlotIndex = 0;
        _hudItemId = null;
        _hudSprite = null;

        draggedItem = null;
        draggedItemOriginalSlot = null;
    }

    public void OnDropZonePointerUp(PointerUpEvent evt)
    {
        if (draggedItem != null)
        {
            ConsumeDraggedItem();
            draggedItem = null;
            draggedItemOriginalSlot = null;
            evt.StopPropagation();
        }
    }

    private void ConsumeDraggedItem()
    {
        if (draggedItem == null)
            return;

        var consumed = draggedItem;
        consumed.RemoveFromHierarchy();
        ItemConsumed?.Invoke(consumed);
    }

    public void ResetDrag()
    {
        draggedItem = null;
        draggedItemOriginalSlot = null;
    }

    private bool IsPointerOverElement(Vector2 pointerPosition, VisualElement element)
    {
        if (element == null)
            return false;
        var rect = element.worldBound;
        return rect.Contains(pointerPosition);
    }
}
