using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Factory for creating inventory item UI elements.
/// </summary>
public static class InventoryItemFactory
{
    public static VisualElement CreateItemElement(Sprite itemSprite, string itemId = null)
    {
        var itemElement = new VisualElement();
        itemElement.name = "InventoryItem";
        itemElement.style.backgroundImage = new StyleBackground(itemSprite);
        itemElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        itemElement.style.width = new Length(100, LengthUnit.Percent);
        itemElement.style.height = new Length(100, LengthUnit.Percent);
        itemElement.style.position = Position.Relative;
        itemElement.style.translate = new Translate(
            new Length(10, LengthUnit.Pixel),
            new Length(-10, LengthUnit.Pixel)
        );
        itemElement.style.rotate = new Rotate(45);
        itemElement.style.alignItems = Align.Center;
        itemElement.style.justifyContent = Justify.Center;
        itemElement.AddToClassList("inventory-item");
        itemElement.pickingMode = PickingMode.Ignore;

        // Attach data for cross-UI interactions (e.g., HUD fast slots assignment).
        // This keeps InventoryController's tooltip map intact while enabling other systems.
        itemElement.userData = new InventoryItemUIData(itemId, itemSprite);

        return itemElement;
    }

    /// <summary>
    /// Resets item styles to fill slot container (relative positioning).
    /// </summary>
    public static void ResetItemStyles(VisualElement item)
    {
        if (item == null)
            return;

        item.style.position = Position.Relative;
        item.style.left = 0;
        item.style.top = 0;
        item.style.width = new Length(100, LengthUnit.Percent);
        item.style.height = new Length(100, LengthUnit.Percent);
    }

    /// <summary>
    /// Sets absolute position for dragging items.
    /// </summary>
    public static void SetAbsolutePosition(
        VisualElement item,
        float left,
        float top,
        float width,
        float height
    )
    {
        if (item == null)
            return;

        item.style.position = Position.Absolute;
        item.style.left = left;
        item.style.top = top;
        item.style.width = width;
        item.style.height = height;
    }

    /// <summary>
    /// Creates a drag ghost element (for cross-panel dragging).
    /// </summary>
    public static VisualElement CreateDragGhost(string itemId, Sprite sprite, float size = 48f)
    {
        if (string.IsNullOrEmpty(itemId))
            return null;

        var ghost = new VisualElement
        {
            name = "DragGhost",
            userData = new InventoryItemUIData(itemId, sprite),
        };

        ghost.style.position = Position.Absolute;
        ghost.style.width = size;
        ghost.style.height = size;

        if (sprite != null)
        {
            ghost.style.backgroundImage = new StyleBackground(sprite);
            ghost.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
            ghost.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            ghost.style.backgroundPositionX = new BackgroundPosition(
                BackgroundPositionKeyword.Center
            );
            ghost.style.backgroundPositionY = new BackgroundPosition(
                BackgroundPositionKeyword.Center
            );
        }

        return ghost;
    }

    /// <summary>
    /// Extracts item data (itemId + sprite) from a VisualElement.
    /// </summary>
    public static bool TryGetItemData(VisualElement element, out string itemId, out Sprite sprite)
    {
        itemId = null;
        sprite = null;

        if (element == null)
            return false;

        if (element.userData is InventoryItemUIData data)
        {
            itemId = data.ItemId;
            sprite = data.Sprite;
            return !string.IsNullOrEmpty(itemId);
        }

        if (element.userData is string s)
        {
            itemId = s;
            return !string.IsNullOrEmpty(itemId);
        }

        return false;
    }
}

/// <summary>
/// Payload attached to inventory item VisualElements via userData.
/// Used for drag-drop operations and cross-UI interactions.
/// </summary>
[Serializable]
public sealed class InventoryItemUIData
{
    public string ItemId;
    public Sprite Sprite;

    public InventoryItemUIData(string itemId, Sprite sprite)
    {
        ItemId = itemId;
        Sprite = sprite;
    }
}
