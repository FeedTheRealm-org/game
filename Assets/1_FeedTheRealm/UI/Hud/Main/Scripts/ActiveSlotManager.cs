using System.Collections.Generic;
using FeedTheRealm.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace FeedTheRealm.UI.Hud
{
    /// <summary>
    /// Manages the active quickslot state, visual indicators, and weapon equipping logic.
    /// Handles item type detection to determine if items should be equipped (weapons) or not (consumables).
    /// </summary>
    public class ActiveSlotManager
    {
        private int _activeSlotIndex = 1; // Default to slot 1
        private const string ACTIVE_SLOT_CLASS = "fast-use-slot-selected";
        private const string INACTIVE_SLOT_CLASS = "fast-use-slot";

        private readonly Dictionary<int, Button> _slotButtons;
        private readonly SlotManager _slotManager;
        private SpriteLoader _spriteLoader;
        private readonly Logging.Logger _logger;
        private readonly int _maxSlotCount;

        public int ActiveSlotIndex => _activeSlotIndex;

        public ActiveSlotManager(
            Dictionary<int, Button> slotButtons,
            SlotManager slotManager,
            SpriteLoader spriteLoader,
            Logging.Logger logger,
            int maxSlotCount
        )
        {
            _slotButtons = slotButtons;
            _slotManager = slotManager;
            _spriteLoader = spriteLoader;
            _logger = logger;
            _maxSlotCount = maxSlotCount;
        }

        /// <summary>
        /// Updates the SpriteLoader reference (called when SpriteLoader becomes available).
        /// </summary>
        public void SetSpriteLoader(SpriteLoader spriteLoader)
        {
            _spriteLoader = spriteLoader;
            _logger?.Log("[ActiveSlotManager] SpriteLoader updated", null);
        }

        /// <summary>
        /// Initializes the default active slot (slot 1) with visual indicator.
        /// </summary>
        public void InitializeDefaultActiveSlot()
        {
            if (_slotButtons.ContainsKey(1))
            {
                SetActiveSlot(1, equipWeapon: false);
            }
        }

        /// <summary>
        /// Activates a slot, updating visual indicators and equipping/unequipping based on content.
        /// </summary>
        public bool TryActivateSlot(int slotIndex)
        {
            // Validate slot index is within configured range
            if (slotIndex < 1 || slotIndex > _maxSlotCount)
            {
                _logger?.Log(
                    $"Cannot activate slot {slotIndex}: out of range (1-{_maxSlotCount})",
                    null,
                    Logging.LogType.Warning
                );
                return false;
            }

            SetActiveSlot(slotIndex, equipWeapon: true);
            return true;
        }

        /// <summary>
        /// Called when an item is assigned to a slot. Auto-equips if it's the active slot and is a weapon.
        /// </summary>
        public void OnSlotItemAssigned(int slotIndex, string itemId)
        {
            if (slotIndex == _activeSlotIndex)
            {
                EquipActiveSlotWeapon();
            }
        }

        /// <summary>
        /// Called when an item is removed from a slot. Unequips if it's the active slot.
        /// </summary>
        public void OnSlotItemRemoved(int slotIndex)
        {
            if (slotIndex == _activeSlotIndex)
            {
                UnequipWeapon();
            }
        }

        /// <summary>
        /// Gets the itemId of the currently active slot, or null if empty.
        /// </summary>
        public string GetActiveSlotItemId()
        {
            if (_slotManager.TryGet(_activeSlotIndex, out var slotData))
            {
                return slotData.ItemId;
            }
            return null;
        }

        /// <summary>
        /// Sets the specified slot as the active slot, updating visual indicators.
        /// </summary>
        private void SetActiveSlot(int slotIndex, bool equipWeapon)
        {
            if (_slotButtons.TryGetValue(_activeSlotIndex, out Button previousButton))
            {
                previousButton?.RemoveFromClassList(ACTIVE_SLOT_CLASS);
                previousButton?.AddToClassList(INACTIVE_SLOT_CLASS);
            }

            _activeSlotIndex = slotIndex;

            if (_slotButtons.TryGetValue(_activeSlotIndex, out Button newButton))
            {
                newButton?.RemoveFromClassList(INACTIVE_SLOT_CLASS);
                newButton?.AddToClassList(ACTIVE_SLOT_CLASS);
            }

            _logger?.Log($"[HUD] Active slot changed to {slotIndex}", null);

            if (equipWeapon)
            {
                EquipActiveSlotWeapon();
            }
        }

        /// <summary>
        /// Equips the weapon from the currently active slot if it has an item and is a weapon type.
        /// </summary>
        private void EquipActiveSlotWeapon()
        {
            if (
                _slotManager.TryGet(_activeSlotIndex, out var slotData)
                && !string.IsNullOrEmpty(slotData.ItemId)
            )
            {
                if (IsWeaponItem(slotData.ItemId))
                {
                    if (_spriteLoader != null)
                    {
                        _logger?.Log(
                            $"[HUD] Auto-equipping weapon from active slot {_activeSlotIndex}: {slotData.ItemId}",
                            null
                        );
                        try
                        {
                            _spriteLoader.EquipWeapon(slotData.ItemId);
                        }
                        catch (System.Exception ex)
                        {
                            _logger?.Log(
                                $"Exception calling EquipWeapon: {ex.Message}\n{ex.StackTrace}",
                                null,
                                Logging.LogType.Error
                            );
                        }
                    }
                    else
                    {
                        _logger?.Log(
                            "SpriteLoader not set, cannot equip weapon sprite",
                            null,
                            Logging.LogType.Warning
                        );
                    }
                }
                else
                {
                    // Active slot has a consumable or other non-weapon item, don't equip
                    _logger?.Log(
                        $"[HUD] Slot {_activeSlotIndex} contains non-weapon item: {slotData.ItemId}, not equipping",
                        null
                    );
                    UnequipWeapon();
                }
            }
            else
            {
                UnequipWeapon();
            }
        }

        /// <summary>
        /// Unequips the current weapon.
        /// </summary>
        private void UnequipWeapon()
        {
            if (_spriteLoader != null)
            {
                _logger?.Log(
                    $"[HUD] Unequipping weapon (active slot {_activeSlotIndex} is empty or has non-weapon)",
                    null
                );
                _spriteLoader.UnequipWeapon();
            }
        }

        /// <summary>
        /// Determines if an item is a weapon type using WorldItemsRegistry.
        /// </summary>
        private bool IsWeaponItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return false;

            var weaponData = Worlds.WorldItemsRegistry.GetWeaponById(itemId);
            return weaponData != null;
        }
    }
}
