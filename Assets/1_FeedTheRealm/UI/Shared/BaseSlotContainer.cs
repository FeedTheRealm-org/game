using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FeedTheRealm.UI.Shared
{
    /// <summary>
    /// Base class for slot-based UI containers (Inventory, HUD quickslots, etc.).
    /// Provides common functionality: slot management, item data tracking, tooltip support,
    /// and drag-item data extraction.
    /// </summary>
    public abstract class BaseSlotContainer : MonoBehaviour
    {
        [Header("Logging")]
        [SerializeField]
        protected Logging.Logger logger;

        [Header("Tooltip (Optional)")]
        [SerializeField]
        [Tooltip("Optional tooltip controller for showing item stats on hover.")]
        protected ItemStatsTooltip itemStatsTooltip;

        [Header("Slot Configuration")]
        [SerializeField]
        [Tooltip(
            "Total number of slots in this container (e.g., 12 for inventory, 5 for HUD quickslots). Max: 9 for HUD quickslots"
        )]
        [Range(1, 12)]
        protected int slotCount = 5;

        protected const string slotNamePrefix = "Slot";
        protected const bool useOneBasedNaming = true;

        // Shared properties
        protected UIDocument uiDocument;
        protected VisualElement root;
        protected SlotManager slotManager;

        // Track itemId for each item VisualElement (used for tooltip and data retrieval)
        protected Dictionary<VisualElement, string> itemIdMap =
            new Dictionary<VisualElement, string>();

        protected virtual void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            slotManager = new SlotManager(logger);
        }

        protected virtual void Start()
        {
            if (uiDocument != null)
            {
                root = uiDocument.rootVisualElement;
            }
        }

        /// <summary>
        /// Returns true if the slot at the given index is empty.
        /// </summary>
        public virtual bool IsSlotEmpty(int slotIndex)
        {
            return slotManager.IsEmpty(slotIndex);
        }

        /// <summary>
        /// Extracts item data (itemId + sprite) from a VisualElement.
        /// Delegates to InventoryItemFactory for consistency.
        /// </summary>
        protected bool TryGetItemDataFromElement(
            VisualElement element,
            out string itemId,
            out Sprite sprite
        )
        {
            // Try factory method first
            if (InventoryItemFactory.TryGetItemData(element, out itemId, out sprite))
                return true;

            // Fallback: try itemIdMap for legacy support
            if (itemIdMap.TryGetValue(element, out itemId))
            {
                sprite = null;
                return !string.IsNullOrEmpty(itemId);
            }

            return false;
        }

        /// <summary>
        /// Common hover enter handler to show tooltip (if available).
        /// </summary>
        protected virtual void OnItemHoverEnter(VisualElement slot)
        {
            if (itemStatsTooltip == null)
                return;

            // For inventory-style slots, check if the slot has a child item element.
            if (slot.childCount > 0)
            {
                var itemElement = slot[0];
                if (itemIdMap.TryGetValue(itemElement, out string itemId))
                {
                    itemStatsTooltip.ShowTooltip(itemId, slot);
                    return;
                }
            }

            // For button-based slots (HUD), try to get itemId from slotManager.
            if (TryGetSlotIndexFromElement(slot, out int slotIndex))
            {
                if (
                    slotManager.TryGet(slotIndex, out var slotData)
                    && !string.IsNullOrEmpty(slotData.ItemId)
                )
                {
                    itemStatsTooltip.ShowTooltip(slotData.ItemId, slot);
                }
            }
        }

        /// <summary>
        /// Common hover leave handler to hide tooltip.
        /// </summary>
        protected virtual void OnItemHoverLeave(VisualElement slot)
        {
            itemStatsTooltip?.HideTooltip();
        }

        /// <summary>
        /// Determines slot index from a VisualElement using configurable naming convention.
        /// Searches up the hierarchy to find a matching element (supports nested structures like Buttons).
        /// </summary>
        protected virtual bool TryGetSlotIndexFromElement(VisualElement element, out int slotIndex)
        {
            slotIndex = -1;
            if (element == null)
                return false;

            // Traverse up the hierarchy to find a matching slot element
            VisualElement current = element;
            while (current != null)
            {
                if (
                    !string.IsNullOrEmpty(current.name)
                    && current.name.StartsWith(
                        slotNamePrefix,
                        System.StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    string suffix = current.name.Substring(slotNamePrefix.Length);
                    if (int.TryParse(suffix, out int parsedIndex))
                    {
                        // Validate index is within range
                        int minIndex = useOneBasedNaming ? 1 : 0;
                        int maxIndex = useOneBasedNaming ? slotCount : slotCount - 1;

                        if (parsedIndex >= minIndex && parsedIndex <= maxIndex)
                        {
                            // Return the index as-is (matching the naming convention)
                            // Each derived class uses the same indexing as their slot names
                            slotIndex = parsedIndex;
                            return true;
                        }
                    }
                }
                current = current.parent;
            }

            return false;
        }

        /// <summary>
        /// Resets item visual styles to fill the slot container (used after drag-drop).
        /// Delegates to InventoryItemFactory for consistency.
        /// </summary>
        protected void ResetItemStyles(VisualElement item)
        {
            InventoryItemFactory.ResetItemStyles(item);
        }

        /// <summary>
        /// Utility: creates a VisualElement for an inventory item with proper styling and userData.
        /// </summary>
        protected VisualElement CreateItemElement(string itemId, Sprite sprite)
        {
            if (sprite == null || string.IsNullOrEmpty(itemId))
                return null;

            var itemElement = InventoryItemFactory.CreateItemElement(sprite, itemId);
            itemIdMap[itemElement] = itemId;
            return itemElement;
        }

        /// <summary>
        /// Removes an item element from tracking when consumed or removed.
        /// </summary>
        protected void UntrackItemElement(VisualElement itemElement)
        {
            if (itemElement != null)
            {
                itemIdMap.Remove(itemElement);
            }
        }
    }
}
