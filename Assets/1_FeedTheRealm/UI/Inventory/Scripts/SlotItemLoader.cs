using FTR.Gameplay.Client.Registry;
using UnityEngine;
using UnityEngine.UIElements;

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
        API.ItemAssetsService itemAssetsService
    )
    {
        if (icon == null)
            return;

        string callMarker = System.Guid.NewGuid().ToString();
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

        if (itemAssetsService == null)
            return;

        var itemData = ClientItemsRegistry.GetItemById(itemId);
        string spriteId =
            itemData != null && !string.IsNullOrEmpty(itemData.spriteFilePath)
                ? itemData.spriteFilePath
                : itemId;
        string categoryName =
            ClientItemsRegistry.GetWeaponById(itemId) != null ? "weapons" : "consumables";

        var texture = await itemAssetsService.DownloadItemSpriteAsync(spriteId, categoryName);

        if (icon.userData as string != callMarker)
            return;
        if (!icon.Contains(itemElement))
            return;
        if (texture == null)
            return;

        itemElement.style.backgroundImage = new StyleBackground(texture);
        itemElement.userData = new InventoryItemUIData(itemId, texture);
    }
}
