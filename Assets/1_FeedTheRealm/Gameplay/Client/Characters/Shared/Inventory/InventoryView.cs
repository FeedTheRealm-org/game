using System;
using FTR.Core.Client.EventChannels.Status;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using UnityEngine;
using VContainer;

/// <summary>
/// Tracks updates on the local player's inventory and notifies the HUD via a static event.
/// </summary>
public class InventoryView : MonoBehaviour
{
    [Inject]
    private LastItemChangedEvent lastItemChangedEvent;

    [Inject]
    private LastSwappedItemChangedEvent lastSwappedItemChangedEvent;

    private InventoryStateStorage stateStorage;

    public void Initialize(InventoryStateStorage stateStorage)
    {
        this.stateStorage = stateStorage;
        stateStorage.OnLastItemChanged += OnInventoryChanged;
        stateStorage.OnLastSwappedItemChanged += OnInventorySwapped;
    }

    private void OnDestroy()
    {
        if (stateStorage != null)
            stateStorage.OnLastItemChanged -= OnInventoryChanged;
        if (stateStorage != null)
            stateStorage.OnLastSwappedItemChanged -= OnInventorySwapped;
    }

    private void OnInventoryChanged(LastItemData value)
    {
        Debug.Log(
            $"InventoryView detected item change: {value.itemId} at position {value.itemPosition}"
        );
        lastItemChangedEvent.Raise((value.itemId, value.itemPosition));
    }

    private void OnInventorySwapped(LastSwappedItemData value)
    {
        Debug.Log(
            $"InventoryView detected item swap: from position {value.sourcePosition} to position {value.targetPosition}"
        );
        lastSwappedItemChangedEvent.Raise((value.sourcePosition, value.targetPosition));
    }
}
