using UnityEngine;
using UnityEngine.UIElements;

namespace FeedTheRealm.UI.Shared
{
    /// <summary>
    /// Data container for a single slot in an inventory-like system.
    /// Encapsulates item ID, sprite, and the visual element representing the slot.
    /// </summary>
    public class SlotData
    {
        public string ItemId { get; set; }
        public Sprite Sprite { get; set; }
        public VisualElement SlotElement { get; set; }

        public SlotData(string itemId, Sprite sprite, VisualElement slotElement)
        {
            ItemId = itemId;
            Sprite = sprite;
            SlotElement = slotElement;
        }

        /// <summary>
        /// Returns true if this slot contains an item (non-empty itemId).
        /// </summary>
        public bool HasItem => !string.IsNullOrEmpty(ItemId);

        /// <summary>
        /// Clears all data from this slot.
        /// </summary>
        public void Clear()
        {
            ItemId = null;
            Sprite = null;
        }
    }
}
