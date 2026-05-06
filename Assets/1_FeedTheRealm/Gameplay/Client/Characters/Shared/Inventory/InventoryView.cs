using System.Threading.Tasks;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

/// <summary>
/// Tracks updates on the local player's inventory and notifies the HUD via event channels.
/// </summary>
public class InventoryView : MonoBehaviour
{
    [Inject]
    private LastAddedEvent lastAddedEvent;

    [Inject]
    private LastSwappedEvent lastSwappedEvent;

    [Inject]
    private LastRemovedEvent lastRemovedEvent;

    [Inject]
    private ActiveSlotChangedEvent ActiveSlotChangedEvent;

    private InventoryStateStorage stateStorage;
    private CharacterStateStorage characterState;

    public void Initialize(InventoryStateStorage stateStorage)
    {
        this.stateStorage = stateStorage;
        stateStorage.OnLastItemChanged += OnInventoryChanged;
        stateStorage.OnLastSwappedItemChanged += OnInventorySwapped;
        stateStorage.OnLastDroppedItemChanged += OnInventoryDropped;
        stateStorage.OnActiveSlotChanged += OnActiveSlotChanged;
    }

    private void OnDestroy()
    {
        if (stateStorage != null)
        {
            stateStorage.OnLastItemChanged -= OnInventoryChanged;
            stateStorage.OnLastSwappedItemChanged -= OnInventorySwapped;
            stateStorage.OnLastDroppedItemChanged -= OnInventoryDropped;
            stateStorage.OnActiveSlotChanged -= OnActiveSlotChanged;
        }
    }

    private void OnInventoryChanged(LastItemData value)
    {
        Debug.Log(
            $"InventoryView item added: {value.itemId} at {value.storageType}[{value.itemPosition}] quantity: {value.quantity}"
        );
        lastAddedEvent.Raise((value.storageType, value.itemId, value.itemPosition, value.quantity));
    }

    private void OnInventorySwapped(LastSwappedItemData value)
    {
        Debug.Log(
            $"InventoryView item swapped: {value.sourceType}[{value.sourcePosition}] <-> {value.targetType}[{value.targetPosition}]"
        );
        lastSwappedEvent.Raise(
            (
                value.sourceType,
                value.sourcePosition,
                value.sourceItemId,
                value.sourceQuantity,
                value.targetType,
                value.targetPosition,
                value.targetItemId,
                value.targetQuantity
            )
        );
    }

    private void OnInventoryDropped(LastItemData value)
    {
        Debug.Log($"InventoryView item dropped: {value.storageType}[{value.itemPosition}]");
        lastRemovedEvent.Raise((value.storageType, value.itemId, value.itemPosition));
    }

    private void OnActiveSlotChanged(int slotIndex)
    {
        Debug.Log($"InventoryView active slot changed: {slotIndex}");
        ActiveSlotChangedEvent.Raise(slotIndex);
    }
}
