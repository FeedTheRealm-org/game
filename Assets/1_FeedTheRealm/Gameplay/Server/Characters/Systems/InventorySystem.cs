using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class InventorySystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private ServerConfig config;

        private uint netId;
        private InventoryStateStorage inventoryState;

        private int inventorySize = 12;
        private int fastSlotSize = 5;

        private string[] inventorySlots;
        private string[] fastSlots;

        public void Initialize(uint netId, InventoryStateStorage inventoryState)
        {
            this.netId = netId;
            this.inventoryState = inventoryState;

            inventorySize = config.InventorySize > 0 ? config.InventorySize : 12;
            fastSlotSize = config.FastSlotSize > 0 ? config.FastSlotSize : 5;

            inventorySlots = new string[inventorySize];
            fastSlots = new string[fastSlotSize];

            for (int i = 0; i < inventorySize; i++)
                inventorySlots[i] = string.Empty;
            for (int i = 0; i < fastSlotSize; i++)
                fastSlots[i] = string.Empty;
        }

        private string[] GetStorage(StorageType type)
        {
            return type == StorageType.FastSlot ? fastSlots : inventorySlots;
        }

        private int GetStorageSize(StorageType type)
        {
            return type == StorageType.FastSlot ? fastSlotSize : inventorySize;
        }

        private bool IsValidSlot(StorageType type, int slot)
        {
            return slot >= 0 && slot < GetStorageSize(type);
        }

        public void OnPickUp(IEventCollectable ec, string itemId, System.Action<bool> onComplete)
        {
            logger.Log($"Attempting to pick up item {itemId} for player {netId}", this);

            for (int i = 0; i < inventorySize; i++)
            {
                if (string.IsNullOrEmpty(inventorySlots[i]))
                {
                    inventorySlots[i] = itemId;
                    inventoryState.AddItem(StorageType.Inventory, i, itemId);
                    logger.Log($"Item {itemId} added to inventory slot {i}", this);
                    onComplete(true);
                    return;
                }
            }

            logger.Log($"Inventory full, cannot pick up item {itemId}", this);
            onComplete(false);
        }

        public void OnMoveItem(
            IEventCollectable ec,
            StorageType sourceType,
            int sourceSlot,
            StorageType targetType,
            int targetSlot
        )
        {
            if (!IsValidSlot(sourceType, sourceSlot) || !IsValidSlot(targetType, targetSlot))
            {
                logger.Log(
                    $"OnMoveItem: invalid slot indices "
                        + $"src={sourceSlot}({sourceType}) tgt={targetSlot}({targetType})",
                    this
                );
                return;
            }

            string[] src = GetStorage(sourceType);
            string[] tgt = GetStorage(targetType);

            string sourceItemId = src[sourceSlot];
            string targetItemId = tgt[targetSlot];

            src[sourceSlot] = targetItemId;
            tgt[targetSlot] = sourceItemId;

            logger.Log(
                $"Swapped player {netId}: "
                    + $"{sourceType}[{sourceSlot}]({sourceItemId}) <-> "
                    + $"{targetType}[{targetSlot}]({targetItemId})",
                this
            );

            inventoryState.SwapItems(sourceType, sourceSlot, targetType, targetSlot);
        }

        public string OnDropItem(IEventCollectable ec, int slotIndex, StorageType storageType)
        {
            if (!IsValidSlot(storageType, slotIndex))
                return null;

            string[] storage = GetStorage(storageType);
            string itemId = storage[slotIndex];
            if (string.IsNullOrEmpty(itemId))
                return null;

            storage[slotIndex] = string.Empty;
            inventoryState.DropItem(storageType, slotIndex);

            logger.Log(
                $"Dropped item {itemId} from {storageType}[{slotIndex}] for player {netId}",
                this
            );
            return itemId;
        }

        public void LoadInventory(string[] inventoryData, string[] fastSlotData)
        {
            // TODO: populate inventorySlots / fastSlots from saved data
            logger.Log($"Loaded inventory for player {netId}", this);
        }

        public void GameTick(float dt) { }
    }
}
