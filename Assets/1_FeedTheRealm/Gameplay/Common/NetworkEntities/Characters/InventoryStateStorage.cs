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
        public int quantity;
        public uint version;

        public LastItemData(
            StorageType storageType,
            int itemPosition,
            string itemId,
            int quantity,
            uint version
        )
        {
            this.storageType = storageType;
            this.itemPosition = itemPosition;
            this.itemId = itemId;
            this.quantity = quantity;
            this.version = version;
        }
    }

    public struct LastSwappedItemData
    {
        public StorageType sourceType;
        public int sourcePosition;
        public string sourceItemId;
        public int sourceQuantity;
        public StorageType targetType;
        public int targetPosition;
        public string targetItemId;
        public int targetQuantity;
        public uint version;

        public LastSwappedItemData(
            StorageType sourceType,
            int sourcePosition,
            string sourceItemId,
            int sourceQuantity,
            StorageType targetType,
            int targetPosition,
            string targetItemId,
            int targetQuantity,
            uint version
        )
        {
            this.sourceType = sourceType;
            this.sourcePosition = sourcePosition;
            this.sourceItemId = sourceItemId;
            this.sourceQuantity = sourceQuantity;
            this.targetType = targetType;
            this.targetPosition = targetPosition;
            this.targetItemId = targetItemId;
            this.targetQuantity = targetQuantity;
            this.version = version;
        }
    }

    // ── NetworkBehaviour ──────────────────────────────────────────────────────

    public class InventoryStateStorage : NetworkBehaviour
    {
        public readonly SyncList<LastItemData> addedItems = new SyncList<LastItemData>();

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

        public LastSwappedItemData LastSwappedItem => lastSwappedItemData;
        public LastItemData LastDroppedItem => lastDroppedItemData;
        public int ActiveSlot => activeSlot;

        public event Action<LastItemData> OnLastItemChanged;
        public event Action<LastSwappedItemData> OnLastSwappedItemChanged;
        public event Action<LastItemData> OnLastDroppedItemChanged;
        public event Action<int> OnActiveSlotChanged;

        public override void OnStartClient()
        {
            addedItems.Callback += OnAddedItemsCallback;

            for (int i = 0; i < addedItems.Count; i++)
                OnLastItemChanged?.Invoke(addedItems[i]);
        }

        private void OnDestroy()
        {
            addedItems.Callback -= OnAddedItemsCallback;
        }

        private void OnAddedItemsCallback(
            SyncList<LastItemData>.Operation op,
            int index,
            LastItemData oldItem,
            LastItemData newItem
        )
        {
            if (op == SyncList<LastItemData>.Operation.OP_ADD)
                OnLastItemChanged?.Invoke(newItem);
        }

        /* --- Setters (server only) --- */

        [Server]
        public void AddItem(StorageType storageType, int position, string itemId, int quantity)
        {
            var data = new LastItemData(storageType, position, itemId, quantity, ++_itemVersion);
            addedItems.Add(data);

            while (addedItems.Count > 50)
                addedItems.RemoveAt(0);
        }

        [Server]
        public void SwapItems(
            StorageType sourceType,
            int sourcePosition,
            string sourceItemId,
            int sourceQuantity,
            StorageType targetType,
            int targetPosition,
            string targetItemId,
            int targetQuantity
        )
        {
            lastSwappedItemData = new LastSwappedItemData(
                sourceType,
                sourcePosition,
                sourceItemId,
                sourceQuantity,
                targetType,
                targetPosition,
                targetItemId,
                targetQuantity,
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
                0,
                ++_dropVersion
            );
        }

        [Server]
        public void SetActiveSlot(int slotIndex)
        {
            activeSlot = slotIndex;
        }

        /* --- SyncVar hooks (client) --- */

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
