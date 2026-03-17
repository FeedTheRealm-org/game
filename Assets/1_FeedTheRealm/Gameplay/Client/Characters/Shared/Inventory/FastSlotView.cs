using System;
using System.Data.SqlTypes;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using UnityEngine;
using VContainer;

/// <summary>
/// Tracks updates on the local player's fast slot and notifies the HUD via a static event.
/// </summary>
public class FastSlotView : MonoBehaviour
{
    // [Inject]
    // private LastAddedEvent lastAddedEvent;

    // [Inject]
    // private LastSwappedEvent lastSwappedEvent;

    // [Inject]
    // private LastRemovedEvent lastRemovedEvent;

    // private FastSlotStateStorage stateStorage;

    // public void Initialize(FastSlotStateStorage stateStorage)
    // {
    //     this.stateStorage = stateStorage;
    //     stateStorage.OnLastItemChanged += OnFastSlotChanged;
    //     stateStorage.OnLastSwappedItemChanged += OnFastSlotSwapped;
    //     stateStorage.OnLastDroppedItemChanged += OnFastSlotDropped;
    // }

    // private void OnDestroy()
    // {
    //     if (stateStorage != null)
    //         stateStorage.OnLastItemChanged -= OnFastSlotChanged;
    //     if (stateStorage != null)
    //         stateStorage.OnLastSwappedItemChanged -= OnFastSlotSwapped;
    //     if (stateStorage != null)
    //         stateStorage.OnLastDroppedItemChanged -= OnFastSlotDropped;
    // }

    // private void OnFastSlotChanged(LastItemData value)
    // {
    //     Debug.Log(
    //         $"FastSlotView detected item change: {value.itemId} at position {value.itemPosition}"
    //     );
    //     lastAddedEvent.Raise((StorageType.FastSlot, value.itemId, value.itemPosition));
    // }

    // private void OnFastSlotSwapped(LastSwappedItemData value)
    // {
    //     Debug.Log(
    //         $"FastSlotView detected item swap: from position {value.sourcePosition} to position {value.targetPosition}"
    //     );
    //     lastSwappedEvent.Raise((StorageType.FastSlot, value.sourcePosition, value.targetPosition));
    // }

    // private void OnFastSlotDropped(LastItemData value)
    // {
    //     Debug.Log(
    //         $"FastSlotView detected item drop: {value.itemId} from position {value.itemPosition}"
    //     );
    //     lastRemovedEvent.Raise((StorageType.FastSlot, value.itemId, value.itemPosition));
    // }
}
