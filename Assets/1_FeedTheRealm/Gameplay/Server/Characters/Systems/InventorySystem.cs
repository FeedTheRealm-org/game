using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server;
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
        private int activeSlot = 0;

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
                    logger.Log(
                        $"[InventorySystem] Item {itemId} added to inventory slot {i}",
                        this
                    );
                    onComplete(true);
                    return;
                }
            }

            logger.Log($"Inventory full, cannot pick up item {itemId}", this);
            onComplete(false);
        }

        public void OnPurchase(IEventCollectable ec, string itemId, int amount)
        {
            logger.Log(
                $"Attempting to add purchased item {itemId} x{amount} to inventory for player {netId}",
                this
            );

            if (amount <= 0)
            {
                logger.Log(
                    $"Invalid purchase amount {amount} for item {itemId} and player {netId}",
                    this
                );
                return;
            }

            int emptySlots = 0;
            for (int i = 0; i < inventorySize; i++)
            {
                if (string.IsNullOrEmpty(inventorySlots[i]))
                    emptySlots++;
            }

            if (emptySlots < amount)
            {
                logger.Log(
                    $"Inventory has insufficient space for purchased item {itemId} x{amount} (free={emptySlots})",
                    this
                );
                return;
            }

            for (int i = 0; i < inventorySize; i++)
            {
                if (string.IsNullOrEmpty(inventorySlots[i]))
                {
                    inventorySlots[i] = itemId;
                    inventoryState.AddItem(StorageType.Inventory, i, itemId);
                    logger.Log(
                        $"[InventorySystem] Purchased item {itemId} added to inventory slot {i}",
                        this
                    );
                    return;
                }
            }

            logger.Log($"Inventory full, cannot add purchased item {itemId}", this);
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

            inventoryState.SwapItems(
                sourceType,
                sourceSlot,
                sourceItemId,
                targetType,
                targetSlot,
                targetItemId
            );
        }

        public string OnDropItem(
            IEventCollectable ec,
            int slotIndex,
            StorageType storageType,
            Vector3 dropPosition,
            ServerPrefabProvider prefabProvider
        )
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

            if (prefabProvider != null)
            {
                SpawnItem(dropPosition, prefabProvider, itemId);
            }

            return itemId;
        }

        private static void SpawnItem(
            Vector3 dropPosition,
            ServerPrefabProvider prefabProvider,
            string itemId
        )
        {
            var lootPrefab = prefabProvider.LootItemPrefab;
            if (lootPrefab != null)
            {
                GameObject lootInstance = Object.Instantiate(
                    lootPrefab,
                    dropPosition,
                    Quaternion.identity
                );
                var stateStorage = lootInstance.GetComponent<LootItemStateStorage>();
                if (stateStorage != null)
                {
                    stateStorage.SetItemId(itemId);
                }
                Mirror.NetworkServer.Spawn(lootInstance);
            }
            else
            {
                Debug.LogWarning(
                    "[InventorySystem] LootItem prefab not found in NetworkManager spawnPrefabs!"
                );
            }
        }

        public void OnEquipItem(IEventCollectable ec, int slotIndex)
        {
            if (!IsValidSlot(StorageType.FastSlot, slotIndex))
            {
                logger.Log($"OnEquipItem: invalid fast slot {slotIndex} for player {netId}", this);
                return;
            }

            activeSlot = slotIndex;
            logger.Log($"Player {netId} equipped fast slot {slotIndex}", this);
            inventoryState.SetActiveSlot(slotIndex);
        }

        public void LoadInventory(string[] inventoryData, string[] fastSlotData)
        {
            // TODO: populate inventorySlots / fastSlots from saved data
            logger.Log($"Loaded inventory for player {netId}", this);
        }

        public void GameTick(float dt) { }
    }
}
