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
        itemElement.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        itemElement.style.width = new Length(100, LengthUnit.Percent);
        itemElement.style.height = new Length(100, LengthUnit.Percent);
        itemElement.style.position = Position.Relative;
        itemElement.style.alignItems = Align.Center;
        itemElement.style.justifyContent = Justify.Center;
        itemElement.AddToClassList("inventory-item");
        itemElement.pickingMode = PickingMode.Ignore;
        // Optionally, you can add itemId as a property or use a dictionary in the controller
        return itemElement;
    }
}
