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

    private InventoryStateStorage stateStorage;

    public void Initialize(InventoryStateStorage stateStorage)
    {
        this.stateStorage = stateStorage;
        stateStorage.OnLastItemChanged += OnInventoryChanged;
    }

    private void OnDestroy()
    {
        if (stateStorage != null)
            stateStorage.OnLastItemChanged -= OnInventoryChanged;
    }

    private void OnInventoryChanged(LastItemData value)
    {
        Debug.Log(
            $"InventoryView detected item change: {value.itemId} at position {value.itemPosition}"
        );
        lastItemChangedEvent.Raise((value.itemId, value.itemPosition));
    }
}
