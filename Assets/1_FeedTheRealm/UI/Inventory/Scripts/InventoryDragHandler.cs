using System.Collections.Generic;
using UnityEngine;
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
    private VisualElement draggedItem;
    private VisualElement draggedItemOriginalSlot;
    private Vector2 dragOffset;

    public InventoryDragHandler(
        VisualElement root,
        List<VisualElement> slots,
        VisualElement dropZone,
        Logging.Logger logger
    )
    {
        this.root = root;
        this.slots = slots;
        this.dropZone = dropZone;
        this.logger = logger;
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
            draggedItem.style.width = itemWidth;
            draggedItem.style.height = itemHeight;
            draggedItem.style.position = Position.Absolute;
            Vector2 pointerInRoot = root.WorldToLocal(evt.position);
            draggedItem.style.left = pointerInRoot.x - dragOffset.x;
            draggedItem.style.top = pointerInRoot.y - dragOffset.y;
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
            draggedItem.style.position = Position.Absolute;
            evt.StopPropagation();
        }
    }

    public void OnSlotPointerUp(
        PointerUpEvent evt,
        System.Action<VisualElement, VisualElement> moveItemToSlot
    )
    {
        if (draggedItem != null)
        {
            var targetSlot = evt.currentTarget as VisualElement;
            if (targetSlot != null && slots.Contains(targetSlot))
            {
                moveItemToSlot(draggedItem, targetSlot);
                draggedItem = null;
                draggedItemOriginalSlot = null;
                evt.StopImmediatePropagation();
            }
        }
    }

    public void OnGlobalPointerUp(PointerUpEvent evt, System.Action returnItemToOriginalSlot)
    {
        if (draggedItem != null)
        {
            bool isOverDrop = IsPointerOverElement(evt.position, dropZone);
            if (isOverDrop)
            {
                draggedItem.RemoveFromHierarchy();
            }
            else
            {
                returnItemToOriginalSlot();
            }
            draggedItem = null;
            draggedItemOriginalSlot = null;
        }
    }

    public void OnDropZonePointerUp(PointerUpEvent evt)
    {
        if (draggedItem != null)
        {
            draggedItem.RemoveFromHierarchy();
            draggedItem = null;
            draggedItemOriginalSlot = null;
            evt.StopPropagation();
        }
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
