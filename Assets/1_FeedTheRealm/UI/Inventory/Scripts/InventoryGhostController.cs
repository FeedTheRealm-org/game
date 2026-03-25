using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages the ghost preview shown when hovering an empty inventory slot
/// while another slot is selected. Belongs to InventoryUIController.
/// </summary>
public class InventorySlotGhostController
{
    private VisualElement currentGhost;
    private VisualElement hoveredIconContainer;

    /// <summary>
    /// Show a ghost preview of the selected item over an empty target slot.
    /// </summary>
    public void OnHoverEnter(
        StorageType selectedStorage,
        int selectedSlotIndex,
        StorageType hoverStorage,
        int hoverIndex,
        System.Func<StorageType, int, VisualElement> getIcon
    )
    {
        if (selectedStorage == StorageType.Null)
            return;
        if (selectedStorage == hoverStorage && selectedSlotIndex == hoverIndex)
            return;

        var targetIcon = getIcon(hoverStorage, hoverIndex);
        if (targetIcon == null)
            return;
        if (targetIcon.childCount > 0)
            return; // only show ghost over empty slots

        var sourceIcon = getIcon(selectedStorage, selectedSlotIndex);
        if (sourceIcon == null || sourceIcon.childCount == 0)
            return;

        var sourceItem = sourceIcon[0];
        if (
            !InventoryItemVisualController.TryGetItemData(
                sourceItem,
                out string itemId,
                out Texture2D texture
            )
        )
            return;

        currentGhost = InventoryItemVisualController.CreateGhostPreview(texture, itemId);
        targetIcon.Add(currentGhost);
        hoveredIconContainer = targetIcon;
    }

    /// <summary>
    /// Remove the ghost preview from wherever it was placed.
    /// </summary>
    public void OnHoverLeave()
    {
        if (currentGhost != null && hoveredIconContainer != null)
        {
            if (hoveredIconContainer.Contains(currentGhost))
                hoveredIconContainer.Remove(currentGhost);
        }
        currentGhost = null;
        hoveredIconContainer = null;
    }
}
