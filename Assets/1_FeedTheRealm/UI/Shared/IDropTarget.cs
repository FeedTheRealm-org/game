using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Interface for drop target containers (Inventory, HUD quickslots, etc.).
/// Enables polymorphic drag-drop operations without tight coupling.
/// </summary>
public interface IDropTarget
{
    /// <summary>
    /// Attempts to place an item in this container at the specified position.
    /// Returns true if item was placed/consumed, false otherwise.
    /// </summary>
    bool TryAcceptItem(
        string itemId,
        Sprite sprite,
        Vector2 screenPosition,
        out ItemPlacementResult result
    );

    /// <summary>
    /// Checks if this container is under the given screen position.
    /// </summary>
    bool IsUnderPosition(Vector2 screenPosition);

    /// <summary>
    /// Returns the container type for logging/debugging.
    /// </summary>
    string ContainerName { get; }
}

/// <summary>
/// Result of an item placement operation.
/// </summary>
public struct ItemPlacementResult
{
    public bool Consumed; // Item was consumed (placed in empty slot)
    public bool Swapped; // Item was swapped with existing item
    public string SwappedItemId; // ID of item that was swapped out
    public Sprite SwappedSprite; // Sprite of item that was swapped out
    public VisualElement TargetSlot; // Target slot element (for returning swapped items)

    public static ItemPlacementResult Failed() =>
        new ItemPlacementResult { Consumed = false, Swapped = false };

    public static ItemPlacementResult PlacedInEmptySlot() =>
        new ItemPlacementResult { Consumed = true, Swapped = false };

    public static ItemPlacementResult SwappedWith(
        string itemId,
        Sprite sprite,
        VisualElement slot
    ) =>
        new ItemPlacementResult
        {
            Consumed = true,
            Swapped = true,
            SwappedItemId = itemId,
            SwappedSprite = sprite,
            TargetSlot = slot,
        };
}

/// <summary>
/// Context data for drag-drop operations.
/// Encapsulates all state needed during drag/drop to reduce parameter passing.
/// </summary>
public struct DragDropContext
{
    public VisualElement DraggedElement;
    public VisualElement OriginalSlot;
    public string ItemId;
    public Sprite ItemSprite;
    public Vector2 ScreenPosition;
    public Vector2 PanelPosition;
    public bool DraggingFromHud;
    public int HudSourceSlotIndex;

    public bool IsValid => DraggedElement != null && !string.IsNullOrEmpty(ItemId);
}
