using System;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Common.NetworkEntities.LootItem
{
    public struct LastItemData
    {
        public string itemId;
        public int itemPosition;

        public LastItemData(string itemId, int itemPosition)
        {
            this.itemId = itemId;
            this.itemPosition = itemPosition;
        }
    }

    public struct LastSwappedItemData
    {
        public int sourcePosition;
        public int targetPosition;

        public LastSwappedItemData(int sourcePosition, int targetPosition)
        {
            this.sourcePosition = sourcePosition;
            this.targetPosition = targetPosition;
        }
    }

    public class InventoryStateStorage : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnLastItemSync))]
        private LastItemData lastItemData;

        [SyncVar(hook = nameof(OnLastSwappedItemSync))]
        private LastSwappedItemData lastSwappedItemData;

        /* --- Getters --- */

        public LastItemData LastItem => lastItemData;
        public LastSwappedItemData LastSwappedItem => lastSwappedItemData;

        public event Action<LastItemData> OnLastItemChanged;
        public event Action<LastSwappedItemData> OnLastSwappedItemChanged;

        /* --- Setters --- */

        [Server]
        public void AddItem(string itemId, int position)
        {
            lastItemData = new LastItemData(itemId, position);
        }

        [Server]
        public void SwapItems(int sourcePosition, int targetPosition)
        {
            lastSwappedItemData = new LastSwappedItemData(sourcePosition, targetPosition);
        }

        private void OnLastItemSync(LastItemData oldLastItemData, LastItemData newLastItemData)
        {
            OnLastItemChanged?.Invoke(newLastItemData);
        }

        private void OnLastSwappedItemSync(LastSwappedItemData oldData, LastSwappedItemData newData)
        {
            OnLastSwappedItemChanged?.Invoke(newData);
        }
    }
}
