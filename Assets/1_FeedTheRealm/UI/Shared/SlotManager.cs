using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FeedTheRealm.UI.Shared
{
    /// <summary>
    /// Manages a collection of slots with item data (composition pattern).
    /// Provides common operations: assignment, clearing, swapping, and querying.
    /// Used by both InventoryController and HudFastUseSlotsController.
    /// </summary>
    public class SlotManager
    {
        private readonly Dictionary<int, SlotData> _slots = new();
        private readonly Logging.Logger _logger;

        public SlotManager(Logging.Logger logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Returns true if the slot at the given index is empty (no itemId).
        /// </summary>
        public bool IsEmpty(int slotIndex)
        {
            return !_slots.TryGetValue(slotIndex, out SlotData data) || !data.HasItem;
        }

        /// <summary>
        /// Assigns an item to the slot at the given index.
        /// </summary>
        public void Assign(int slotIndex, string itemId, Sprite sprite, VisualElement slotElement)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                _logger?.Log(
                    $"[SlotManager] Cannot assign empty itemId to slot {slotIndex}",
                    null,
                    Logging.LogType.Warning
                );
                return;
            }

            if (!_slots.ContainsKey(slotIndex))
            {
                _slots[slotIndex] = new SlotData(itemId, sprite, slotElement);
            }
            else
            {
                var slot = _slots[slotIndex];
                slot.ItemId = itemId;
                slot.Sprite = sprite;
                slot.SlotElement = slotElement;
            }

            _logger?.Log($"[SlotManager] Assigned itemId={itemId} to slot {slotIndex}", null);
        }

        /// <summary>
        /// Clears the item data from the slot at the given index.
        /// </summary>
        public void Clear(int slotIndex)
        {
            if (_slots.TryGetValue(slotIndex, out SlotData data))
            {
                data.Clear();
                _logger?.Log($"[SlotManager] Cleared slot {slotIndex}", null);
            }
        }

        /// <summary>
        /// Attempts to retrieve the slot data at the given index.
        /// </summary>
        public bool TryGet(int slotIndex, out SlotData data)
        {
            if (_slots.TryGetValue(slotIndex, out data) && data.HasItem)
            {
                return true;
            }

            data = null;
            return false;
        }

        /// <summary>
        /// Takes the item from the slot (returns data and clears the slot).
        /// </summary>
        public bool TryTake(int slotIndex, out string itemId, out Sprite sprite)
        {
            itemId = null;
            sprite = null;

            if (!TryGet(slotIndex, out SlotData data))
                return false;

            itemId = data.ItemId;
            sprite = data.Sprite;
            Clear(slotIndex);
            return true;
        }

        /// <summary>
        /// Swaps the items between two slots.
        /// </summary>
        public void Swap(int slotIndexA, int slotIndexB)
        {
            var hasA = TryGet(slotIndexA, out SlotData dataA);
            var hasB = TryGet(slotIndexB, out SlotData dataB);

            if (!hasA && !hasB)
                return; // Both empty, nothing to swap.

            string tempItemId = hasA ? dataA.ItemId : null;
            Sprite tempSprite = hasA ? dataA.Sprite : null;
            VisualElement elementA = hasA ? dataA.SlotElement : null;
            VisualElement elementB = hasB ? dataB.SlotElement : null;

            if (hasB)
            {
                Assign(slotIndexA, dataB.ItemId, dataB.Sprite, elementA);
            }
            else
            {
                Clear(slotIndexA);
            }

            if (hasA)
            {
                Assign(slotIndexB, tempItemId, tempSprite, elementB);
            }
            else
            {
                Clear(slotIndexB);
            }

            _logger?.Log($"[SlotManager] Swapped slots {slotIndexA} <-> {slotIndexB}", null);
        }

        /// <summary>
        /// Returns all slot indices that have items.
        /// </summary>
        public IEnumerable<int> GetOccupiedSlotIndices()
        {
            foreach (var kvp in _slots)
            {
                if (kvp.Value.HasItem)
                {
                    yield return kvp.Key;
                }
            }
        }

        /// <summary>
        /// Returns the total count of registered slots.
        /// </summary>
        public int TotalSlotCount => _slots.Count;

        /// <summary>
        /// Registers a slot element (without assigning an item yet).
        /// </summary>
        public void RegisterSlot(int slotIndex, VisualElement slotElement)
        {
            if (!_slots.ContainsKey(slotIndex))
            {
                _slots[slotIndex] = new SlotData(null, null, slotElement);
            }
            else
            {
                _slots[slotIndex].SlotElement = slotElement;
            }
        }
    }
}
