using System;
using FTR.Core.Common.Protocol.RpcMessages;
using Mirror;

namespace FTR.Gameplay.Common.NetworkEntities.LootItem
{
    // ── Shared structs ────────────────────────────────────────────────────────

    public struct LastItemData
    {
        public StorageType storageType;
        public int itemPosition;
        public string itemId;

        public LastItemData(StorageType storageType, int itemPosition, string itemId)
        {
            this.storageType = storageType;
            this.itemPosition = itemPosition;
            this.itemId = itemId;
        }
    }

    public struct LastSwappedItemData
    {
        public int sourcePosition;
        public StorageType sourceType;
        public int targetPosition;
        public StorageType targetType;

        public LastSwappedItemData(
            int sourcePosition,
            StorageType sourceType,
            int targetPosition,
            StorageType targetType
        )
        {
            this.sourcePosition = sourcePosition;
            this.sourceType = sourceType;
            this.targetPosition = targetPosition;
            this.targetType = targetType;
        }
    }

    // ── NetworkBehaviour ──────────────────────────────────────────────────────

    public class InventoryStateStorage : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnLastItemSync))]
        private LastItemData lastItemData;

        [SyncVar(hook = nameof(OnLastSwappedItemSync))]
        private LastSwappedItemData lastSwappedItemData;

        [SyncVar(hook = nameof(OnLastDroppedItemSync))]
        private LastItemData lastDroppedItemData;

        /* --- Getters --- */

        public LastItemData LastItem => lastItemData;
        public LastSwappedItemData LastSwappedItem => lastSwappedItemData;
        public LastItemData LastDroppedItem => lastDroppedItemData;

        public event Action<LastItemData> OnLastItemChanged;
        public event Action<LastSwappedItemData> OnLastSwappedItemChanged;
        public event Action<LastItemData> OnLastDroppedItemChanged;

        /* --- Setters (server only) --- */

        [Server]
        public void AddItem(StorageType storageType, int position, string itemId)
        {
            lastItemData = new LastItemData(storageType, position, itemId);
        }

        [Server]
        public void SwapItems(
            StorageType sourceType,
            int sourcePosition,
            StorageType targetType,
            int targetPosition
        )
        {
            lastSwappedItemData = new LastSwappedItemData(
                sourcePosition,
                sourceType,
                targetPosition,
                targetType
            );
        }

        [Server]
        public void DropItem(StorageType storageType, int position)
        {
            lastDroppedItemData = new LastItemData(storageType, position, string.Empty);
        }

        /* --- SyncVar hooks (client) --- */

        private void OnLastItemSync(LastItemData oldData, LastItemData newData) =>
            OnLastItemChanged?.Invoke(newData);

        private void OnLastSwappedItemSync(
            LastSwappedItemData oldData,
            LastSwappedItemData newData
        ) => OnLastSwappedItemChanged?.Invoke(newData);

        private void OnLastDroppedItemSync(LastItemData oldData, LastItemData newData) =>
            OnLastDroppedItemChanged?.Invoke(newData);
    }
}
