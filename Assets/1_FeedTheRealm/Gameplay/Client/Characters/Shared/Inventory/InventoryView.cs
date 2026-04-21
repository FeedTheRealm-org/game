using System.Threading.Tasks;
using FTR.Core.Client.EventChannels.Inventory;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
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
    private API.ItemAssetsService itemsAssetsService;

    private InventoryStateStorage stateStorage;
    private CharacterStateStorage characterState;
    private SpriteManager spriteManager;

    public void Initialize(
        InventoryStateStorage stateStorage,
        CharacterStateStorage characterState,
        SpriteManager spriteManager
    )
    {
        this.spriteManager = spriteManager;
        this.stateStorage = stateStorage;
        this.characterState = characterState;
        stateStorage.OnLastItemChanged += OnInventoryChanged;
        stateStorage.OnLastSwappedItemChanged += OnInventorySwapped;
        stateStorage.OnLastDroppedItemChanged += OnInventoryDropped;
        stateStorage.OnActiveSlotChanged += OnActiveSlotChanged;
        characterState.OnEquippedItemChanged += OnEquippedItemChanged;
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
        if (characterState != null)
        {
            characterState.OnEquippedItemChanged -= OnEquippedItemChanged;
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

    private void OnEquippedItemChanged(string itemId)
    {
        _ = ApplyEquippedItemAsync(itemId);
    }

    private async Task ApplyEquippedItemAsync(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.Log($"InventoryView equipped item removed");
            spriteManager.ChangeSprite(CharacterPartCategory.EquipmentR, null);
            return;
        }

        var itemData = ClientItemsRegistry.GetItemById(itemId);
        string spriteId =
            itemData != null && !string.IsNullOrEmpty(itemData.spriteFilePath)
                ? itemData.spriteFilePath
                : itemId;

        Debug.Log($"InventoryView applying equipped item: {itemId} with spriteId: {spriteId}");
        var texture = await itemsAssetsService.DownloadItemSpriteAsync(spriteId);

        if (this == null || spriteManager == null)
        {
            Debug.Log(
                $"InventoryView no longer valid after sprite download, aborting apply for {itemId}"
            );
            return;
        }

        spriteManager.ChangeSprite(CharacterPartCategory.EquipmentR, texture);
    }
}
