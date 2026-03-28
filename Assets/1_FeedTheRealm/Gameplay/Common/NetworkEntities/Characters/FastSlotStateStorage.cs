using System;
using Mirror;

namespace FTR.Gameplay.Common.NetworkEntities.LootItem
{
    public class FastSlotStateStorage : NetworkBehaviour
    {
        // [SyncVar(hook = nameof(OnLastItemSync))]
        // private LastItemData lastItemData;

        // [SyncVar(hook = nameof(OnLastSwappedItemSync))]
        // private LastSwappedItemData lastSwappedItemData;

        // [SyncVar(hook = nameof(OnLastDroppedItemSync))]
        // private LastItemData lastDroppedItemData;

        // public LastItemData LastItem => lastItemData;
        // public LastSwappedItemData LastSwappedItem => lastSwappedItemData;
        // public LastItemData LastDroppedItem => lastDroppedItemData;

        // public event Action<LastItemData> OnLastItemChanged;
        // public event Action<LastSwappedItemData> OnLastSwappedItemChanged;
        // public event Action<LastItemData> OnLastDroppedItemChanged;

        // [Server]
        // public void AddItem(string itemId, int position)
        // {
        //     lastItemData = new LastItemData(itemId, position);
        // }

        // [Server]
        // public void SwapItems(int sourcePosition, int targetPosition)
        // {
        //     lastSwappedItemData = new LastSwappedItemData(sourcePosition, targetPosition);
        // }

        // [Server]
        // public void DropItem(int position)
        // {
        //     lastDroppedItemData = new LastItemData(string.Empty, position);
        // }

        // private void OnLastItemSync(LastItemData oldData, LastItemData newData)
        // {
        //     OnLastItemChanged?.Invoke(newData);
        // }

        // private void OnLastSwappedItemSync(LastSwappedItemData oldData, LastSwappedItemData newData)
        // {
        //     OnLastSwappedItemChanged?.Invoke(newData);
        // }

        // private void OnLastDroppedItemSync(LastItemData oldData, LastItemData newData)
        // {
        //     OnLastDroppedItemChanged?.Invoke(newData);
        // }
    }
}
