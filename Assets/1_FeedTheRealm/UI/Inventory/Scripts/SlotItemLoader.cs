using System;
using FTR.Core.Client.EntryPoints;
using FTR.Gameplay.Client.Registry;
using FTRShared.Runtime.Core.Cache;
using UnityEngine;
using UnityEngine.UIElements;

namespace FTR.UI.Inventory
{
    /// <summary>
    /// Shared async loader for inventory slot item textures.
    /// Handles stale-call cancellation via a callMarker stored in icon.userData.
    /// Used by both InventoryUIController and FastSlotUIController.
    /// </summary>
    public static class SlotItemLoader
    {
        /// <summary>
        /// Clears the slot if itemId is null/empty, otherwise downloads and applies
        /// the item texture. Cancels automatically if the slot is reassigned before
        /// the download completes.
        /// </summary>
        public static async void LoadItem(
            VisualElement icon,
            string itemId,
            CacheManager cacheManager,
            WorldSelector worldSelector = null,
            int quantity = 1
        )
        {
            if (icon == null)
                return;

            string callMarker = System.Guid.NewGuid().ToString();
            string short_ = callMarker.Substring(0, 6);
            Debug.Log(
                $"[SlotItemLoader] START icon={icon.name} itemId={itemId ?? "NULL"} marker={short_}"
            );

            icon.userData = callMarker;
            icon.Clear();
            icon.style.backgroundImage = null;

            if (string.IsNullOrEmpty(itemId))
            {
                icon.userData = null;
                return;
            }

            var itemElement = InventoryItemVisualController.CreateItemElement(null, itemId);
            icon.Add(itemElement);

            InventoryItemVisualController.SetStackCount(itemElement, quantity);

            if (cacheManager == null)
                return;

            var itemData = ClientItemsRegistry.GetItemById(itemId);
            string spriteReference =
                itemData != null && !string.IsNullOrEmpty(itemData.spriteFilePath)
                    ? itemData.spriteFilePath
                    : itemId;

            string fileName = System.IO.Path.GetFileName(spriteReference);
            spriteReference = $"/worlds/{worldSelector.GetSelectedWorldId()}/items/{fileName}";

            var texture = await cacheManager.GetSprite(
                spriteReference,
                worldSelector.GetSelectedWorldUpdatedAt()
            );

            string currentMarker = icon.userData as string;
            Debug.Log(
                $"[SlotItemLoader] POST-AWAIT icon={icon.name} itemId={itemId} marker={short_} "
                    + $"currentMarker={currentMarker?.Substring(0, Math.Min(6, currentMarker?.Length ?? 0)) ?? "NULL"} "
                    + $"match={currentMarker == callMarker} contains={icon.Contains(itemElement)}"
            );

            if (currentMarker != callMarker)
                return;
            if (!icon.Contains(itemElement))
                return;
            if (texture == null)
                return;

            itemElement.style.backgroundImage = new StyleBackground(texture);
            itemElement.userData = new InventoryItemUIData(itemId, texture);
            InventoryItemVisualController.SetStackCount(itemElement, quantity);
            Debug.Log($"[SlotItemLoader] SUCCESS icon={icon.name} itemId={itemId}");
        }
    }
}
