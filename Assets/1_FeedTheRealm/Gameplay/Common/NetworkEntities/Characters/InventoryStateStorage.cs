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
        public uint version;

        public LastItemData(StorageType storageType, int itemPosition, string itemId, uint version)
        {
            this.storageType = storageType;
            this.itemPosition = itemPosition;
            this.itemId = itemId;
            this.version = version;
        }
    }

    public struct LastSwappedItemData
    {
        public StorageType sourceType;
        public int sourcePosition;
        public string sourceItemId;
        public StorageType targetType;
        public int targetPosition;
        public string targetItemId;
        public uint version;

        public LastSwappedItemData(
            StorageType sourceType,
            int sourcePosition,
            string sourceItemId,
            StorageType targetType,
            int targetPosition,
            string targetItemId,
            uint version
        )
        {
            this.sourceType = sourceType;
            this.sourcePosition = sourcePosition;
            this.sourceItemId = sourceItemId;
            this.targetType = targetType;
            this.targetPosition = targetPosition;
            this.targetItemId = targetItemId;
            this.version = version;
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

        [SyncVar(hook = nameof(OnActiveSlotSync))]
        private int activeSlot = 0;

        private uint _itemVersion = 0;
        private uint _swapVersion = 0;
        private uint _dropVersion = 0;

        /* --- Getters --- */

        public LastItemData LastItem => lastItemData;
        public LastSwappedItemData LastSwappedItem => lastSwappedItemData;
        public LastItemData LastDroppedItem => lastDroppedItemData;
        public int ActiveSlot => activeSlot;

        public event Action<LastItemData> OnLastItemChanged;
        public event Action<LastSwappedItemData> OnLastSwappedItemChanged;
        public event Action<LastItemData> OnLastDroppedItemChanged;
        public event Action<int> OnActiveSlotChanged;

        /* --- Setters (server only) --- */

        [Server]
        public void AddItem(StorageType storageType, int position, string itemId)
        {
            lastItemData = new LastItemData(storageType, position, itemId, ++_itemVersion);
        }

        [Server]
        public void SwapItems(
            StorageType sourceType,
            int sourcePosition,
            string sourceItemId,
            StorageType targetType,
            int targetPosition,
            string targetItemId
        )
        {
            lastSwappedItemData = new LastSwappedItemData(
                sourceType,
                sourcePosition,
                sourceItemId,
                targetType,
                targetPosition,
                targetItemId,
                ++_swapVersion
            );
        }

        [Server]
        public void DropItem(StorageType storageType, int position)
        {
            lastDroppedItemData = new LastItemData(
                storageType,
                position,
                string.Empty,
                ++_dropVersion
            );
        }

        [Server]
        public void SetActiveSlot(int slotIndex)
        {
            activeSlot = slotIndex;
        }

        /* --- SyncVar hooks (client) --- */

        private void OnLastItemSync(LastItemData oldData, LastItemData newData)
        {
            OnLastItemChanged?.Invoke(newData);
        }

        private void OnLastSwappedItemSync(LastSwappedItemData oldData, LastSwappedItemData newData)
        {
            OnLastSwappedItemChanged?.Invoke(newData);
        }

        private void OnLastDroppedItemSync(LastItemData oldData, LastItemData newData)
        {
            OnLastDroppedItemChanged?.Invoke(newData);
        }

        private void OnActiveSlotSync(int oldSlot, int newSlot)
        {
            OnActiveSlotChanged?.Invoke(newSlot);
        }
    }
}
