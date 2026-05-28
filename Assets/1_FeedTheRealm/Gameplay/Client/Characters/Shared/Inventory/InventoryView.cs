using System;
using System.Threading.Tasks;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Core.Common.Protocol.RpcMessages;
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

    [Inject]
    private InventoryErrorEvent inventoryErrorEventChannel;

    [Inject]
    private ISoundPlayer soundPlayer;

    private InventoryStateStorage stateStorage;
    private NetworkEventRouter eventRouter;

    public void Initialize(InventoryStateStorage stateStorage, NetworkEventRouter eventRouter)
    {
        this.stateStorage = stateStorage;
        this.eventRouter = eventRouter;
        stateStorage.OnLastItemChanged += OnInventoryChanged;
        stateStorage.OnLastSwappedItemChanged += OnInventorySwapped;
        stateStorage.OnLastDroppedItemChanged += OnInventoryDropped;
        stateStorage.OnActiveSlotChanged += OnActiveSlotChanged;
        eventRouter.OnShopPurchaseConfirmEvent += OnShopPurchaseConfirm;
        eventRouter.OnLootedItemConfirmEvent += OnLootedItemConfirm;
        eventRouter.OnInventoryErrorEvent += HandleInventoryErrorEvent;
    }

    private void HandleInventoryErrorEvent(InventoryErrorContent content)
    {
        inventoryErrorEventChannel?.Raise(content.ErrorType);
    }

    private void OnLootedItemConfirm()
    {
        Debug.Log("InventoryView received lootbag pickup confirm event");
        soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.Pickup);
    }

    private void OnShopPurchaseConfirm()
    {
        soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.Purchase);
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
        if (eventRouter != null)
        {
            eventRouter.OnShopPurchaseConfirmEvent -= OnShopPurchaseConfirm;
            eventRouter.OnLootedItemConfirmEvent -= OnLootedItemConfirm;
            eventRouter.OnInventoryErrorEvent -= HandleInventoryErrorEvent;
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
