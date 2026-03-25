using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FTR.UI.Inventory
{
    /// <summary>
    /// Factory for creating inventory item UI elements.
    /// </summary>
    public static class InventoryItemVisualController
    {
        public static VisualElement CreateItemElement(
            Texture2D itemTexture,
            string itemId = null,
            float opacity = 1f
        )
        {
            var itemElement = new VisualElement();
            itemElement.name = "InventoryItem";
            if (itemTexture != null)
                itemElement.style.backgroundImage = new StyleBackground(itemTexture);

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
            itemElement.style.opacity = opacity;
            itemElement.AddToClassList("inventory-item");
            itemElement.pickingMode = PickingMode.Ignore;

            itemElement.userData = new InventoryItemUIData(itemId, itemTexture);

            return itemElement;
        }

        /// <summary>
        /// Creates a drag ghost element or a preview for hovering empty slots.
        /// </summary>
        public static VisualElement CreateGhostPreview(Texture2D texture, string itemId = null)
        {
            var ghost = CreateItemElement(texture, itemId, 0.5f);
            ghost.name = "GhostPreview";
            return ghost;
        }

        /// <summary>
        /// Extracts item data (itemId + texture) from a VisualElement.
        /// </summary>
        public static bool TryGetItemData(
            VisualElement element,
            out string itemId,
            out Texture2D texture
        )
        {
            itemId = null;
            texture = null;

            if (element == null)
                return false;

            if (element.userData is InventoryItemUIData data)
            {
                itemId = data.ItemId;
                texture = data.Texture;
                return !string.IsNullOrEmpty(itemId);
            }

            return false;
        }
    }

    /// <summary>
    /// Payload attached to inventory item VisualElements via userData.
    /// </summary>
    [Serializable]
    public sealed class InventoryItemUIData
    {
        public string ItemId;
        public Texture2D Texture;

        public InventoryItemUIData(string itemId, Texture2D texture)
        {
            ItemId = itemId;
            Texture = texture;
        }
    }
}
