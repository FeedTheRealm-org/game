using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Mediator for drag-drop operations between multiple containers.
/// Eliminates conditional logic by using polymorphic IDropTarget interface.
/// </summary>
public class DragDropMediator
{
    private readonly List<IDropTarget> dropTargets = new List<IDropTarget>();
    private readonly VisualElement dropZone;
    private readonly Logging.Logger logger;

    public DragDropMediator(VisualElement dropZone, Logging.Logger logger)
    {
        this.dropZone = dropZone;
        this.logger = logger;
    }

    /// <summary>
    /// Registers a drop target (Inventory, HUD, etc.).
    /// </summary>
    public void RegisterDropTarget(IDropTarget target)
    {
        if (target != null && !dropTargets.Contains(target))
        {
            dropTargets.Add(target);
            logger?.Log($"[DragDropMediator] Registered drop target: {target.ContainerName}", null);
        }
    }

    /// <summary>
    /// Unregisters a drop target.
    /// </summary>
    public void UnregisterDropTarget(IDropTarget target)
    {
        dropTargets.Remove(target);
    }

    /// <summary>
    /// Attempts to drop an item at the given position.
    /// Returns true if item was handled, along with placement result.
    /// </summary>
    public bool TryDropItem(DragDropContext context, out ItemPlacementResult result)
    {
        result = ItemPlacementResult.Failed();

        if (!context.IsValid)
            return false;

        // Check if dropping in drop zone (consume item)
        if (IsOverDropZone(context.PanelPosition))
        {
            result = ItemPlacementResult.PlacedInEmptySlot();
            result.Consumed = true;
            logger?.Log("[DragDropMediator] Item dropped in drop zone (consumed)", null);
            return true;
        }

        // Try each registered drop target
        foreach (var target in dropTargets)
        {
            if (
                target.TryAcceptItem(
                    context.ItemId,
                    context.ItemSprite,
                    context.ScreenPosition,
                    out result
                )
            )
            {
                logger?.Log(
                    $"[DragDropMediator] Item accepted by {target.ContainerName} (Consumed: {result.Consumed}, Swapped: {result.Swapped})",
                    null
                );
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if position is over the drop zone (consume area).
    /// </summary>
    private bool IsOverDropZone(Vector2 panelPosition)
    {
        if (dropZone == null)
            return false;

        return dropZone.worldBound.Contains(panelPosition);
    }
}
