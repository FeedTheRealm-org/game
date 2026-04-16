using System;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Core.Server.Persistence.Schemas;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class InventorySystem : MonoBehaviour, IGameTickable
    {
        public event Action<InventoryItemModel[], InventoryItemModel[]> OnSaveInventory;

        [SerializeField]
        private Logging.Logger logger;

        private ServerConfig config;

        [Inject]
        public void Construct(IObjectResolver resolver, ServerConfig config)
        {
            if (resolver.TryResolve<QuestRewardItemEvent>(out var ev) && ev != null)
                questRewardItemEvent = ev;
            this.config = config;
        }

        private QuestRewardItemEvent questRewardItemEvent;
        private bool subscribedToQuestReward = false;

        private uint netId;
        private InventoryStateStorage inventoryState;

        private int activeSlot = 0;

        private InventoryItemModel[] inventorySlots;
        private InventoryItemModel[] fastSlots;

        public void Initialize(uint netId, InventoryStateStorage inventoryState)
        {
            this.netId = netId;
            this.inventoryState = inventoryState;
            SubscribeToQuestReward();
        }

        private void OnDestroy()
        {
            UnsubscribeFromQuestReward();
        }

        private void OnQuestRewardItem((uint playerNetId, string itemId) data)
        {
            if (data.playerNetId != netId)
                return;

            logger?.Log(
                $"[InventorySystem] Quest reward: adding item '{data.itemId}' to Player:{netId}.",
                this
            );

            AddItemToInventory(data.itemId);
        }

        public void OnPickUp(IEventCollectable ec, string itemId, System.Action<bool> onComplete)
        {
            logger.Log($"Attempting to pick up item {itemId} for player {netId}", this);
            if (string.IsNullOrEmpty(itemId))
            {
                logger.Log($"Invalid itemId '{itemId}' for player {netId}", this);
                onComplete(false);
                return;
            }

            bool added = AddItemToInventory(itemId);
            if (added)
                logger.Log($"Picked up item {itemId} for player {netId}", this);
            else
                logger.Log($"Inventory full, cannot pick up item {itemId}", this);

            onComplete(added);
        }

        public bool OnPurchase(IEventCollectable ec, string itemId, int amount)
        {
            logger.Log(
                $"[InventorySystem] Purchase: item {itemId} x{amount} for Player:{netId}",
                this
            );

            if (amount <= 0)
            {
                logger.Log(
                    $"[InventorySystem] Invalid purchase amount {amount} for item {itemId}",
                    this
                );
                return false;
            }

            if (!CanAddItemToInventory(itemId))
            {
                logger.Log(
                    $"Inventory has insufficient space for purchased item {itemId} x {amount}",
                    this
                );
                return false;
            }

            AddItemToInventory(itemId, amount);

            logger.Log(
                $"[InventorySystem] Purchased item {itemId} x{amount} added to inventory for Player:{netId}",
                this
            );
            return true;
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
                    $"OnMoveItem: invalid slots "
                        + $"src={sourceSlot}({sourceType}) tgt={targetSlot}({targetType})",
                    this
                );
                return;
            }

            InventoryItemModel[] src = GetStorage(sourceType);
            InventoryItemModel[] tgt = GetStorage(targetType);

            var sourceItem = src[sourceSlot];
            var targetItem = tgt[targetSlot];

            src[sourceSlot] = targetItem;
            tgt[targetSlot] = sourceItem;

            logger.Log(
                $"Swapped Player:{netId}: "
                    + $"{sourceType}[{sourceSlot}]({sourceItem.ItemId}) <-> "
                    + $"{targetType}[{targetSlot}]({targetItem.ItemId})",
                this
            );

            inventoryState.SwapItems(
                sourceType,
                sourceSlot,
                sourceItem.ItemId,
                targetType,
                targetSlot,
                targetItem.ItemId
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

            InventoryItemModel[] storage = GetStorage(storageType);
            var item = storage[slotIndex];
            if (string.IsNullOrEmpty(item.ItemId))
                return null;

            storage[slotIndex].ItemId = string.Empty;
            storage[slotIndex].Quantity = 0;
            inventoryState.DropItem(storageType, slotIndex);

            logger.Log(
                $"Dropped item {item} from {storageType}[{slotIndex}] for Player:{netId}",
                this
            );

            if (prefabProvider != null)
                SpawnItem(dropPosition, prefabProvider, item.ItemId);

            return item.ItemId;
        }

        public void OnEquipItem(IEventCollectable ec, int slotIndex)
        {
            if (!IsValidSlot(StorageType.FastSlot, slotIndex))
            {
                logger.Log($"OnEquipItem: invalid fast slot {slotIndex}", this);
                return;
            }

            activeSlot = slotIndex;
            logger.Log($"Player:{netId} equipped fast slot {slotIndex}", this);
            inventoryState.SetActiveSlot(slotIndex);
        }

        public void LoadInventory(
            InventoryItemModel[] inventoryData,
            InventoryItemModel[] fastSlotData
        )
        {
            this.inventorySlots = inventoryData;
            this.fastSlots = fastSlotData;

            // Sync them to the inventory state storage
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                var item = inventorySlots[i];
                if (!string.IsNullOrEmpty(item.ItemId))
                    inventoryState.AddItem(StorageType.Inventory, i, item.ItemId, item.Quantity);
            }
            for (int i = 0; i < fastSlots.Length; i++)
            {
                var item = fastSlots[i];
                if (!string.IsNullOrEmpty(item.ItemId))
                    inventoryState.AddItem(StorageType.FastSlot, i, item.ItemId, item.Quantity);
            }

            logger.Log($"Loaded inventory for Player:{netId}", this);
        }

        public void GameTick(float dt) { }

        // ── Private helpers ───────────────────────────────────────────────────

        private bool CanAddItemToInventory(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return false;

            int emptySlotCount = 0;
            bool hasExistingStack = false;
            for (int i = 0; i < config.InventorySize; i++)
            {
                if (string.IsNullOrEmpty(inventorySlots[i].ItemId))
                    emptySlotCount++;
                else if (inventorySlots[i].ItemId == itemId)
                    hasExistingStack = true;
            }

            return emptySlotCount > 0 || hasExistingStack;
        }

        private bool AddItemToInventory(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId))
                return false;

            int emptySlotIndex = -1;
            bool wasAdded = false;
            for (int i = 0; i < config.InventorySize; i++)
            {
                if (string.IsNullOrEmpty(inventorySlots[i].ItemId) && emptySlotIndex < 0)
                {
                    emptySlotIndex = i;
                }
                else if (inventorySlots[i].ItemId == itemId)
                {
                    inventorySlots[i].Quantity += quantity;
                    inventoryState.AddItem(
                        StorageType.Inventory,
                        i,
                        itemId,
                        inventorySlots[i].Quantity
                    );
                    wasAdded = true;
                    break;
                }
            }

            if (!wasAdded && emptySlotIndex >= 0)
            {
                inventorySlots[emptySlotIndex].ItemId = itemId;
                inventorySlots[emptySlotIndex].Quantity = quantity;
                inventoryState.AddItem(StorageType.Inventory, emptySlotIndex, itemId, quantity);
                wasAdded = true;
            }

            return wasAdded;
        }

        private InventoryItemModel[] GetStorage(StorageType type) =>
            type == StorageType.FastSlot ? fastSlots : inventorySlots;

        private int GetStorageSize(StorageType type) =>
            type == StorageType.FastSlot ? config.FastSlotSize : config.InventorySize;

        private bool IsValidSlot(StorageType type, int slot) =>
            slot >= 0 && slot < GetStorageSize(type);

        private static void SpawnItem(
            Vector3 dropPosition,
            ServerPrefabProvider prefabProvider,
            string itemId
        )
        {
            var lootPrefab = prefabProvider.LootItemPrefab;
            if (lootPrefab == null)
            {
                Debug.LogWarning("[InventorySystem] LootItem prefab not assigned.");
                return;
            }

            var lootInstance = UnityEngine.Object.Instantiate(
                lootPrefab,
                dropPosition,
                Quaternion.identity
            );
            var stateStorage = lootInstance.GetComponent<LootItemStateStorage>();
            if (stateStorage != null)
                stateStorage.SetItemId(itemId);

            Mirror.NetworkServer.Spawn(lootInstance);
        }

        private void SubscribeToQuestReward()
        {
            if (subscribedToQuestReward || questRewardItemEvent == null)
                return;
            questRewardItemEvent.OnRaised += OnQuestRewardItem;
            subscribedToQuestReward = true;
        }

        private void UnsubscribeFromQuestReward()
        {
            if (!subscribedToQuestReward || questRewardItemEvent == null)
                return;
            questRewardItemEvent.OnRaised -= OnQuestRewardItem;
            subscribedToQuestReward = false;
        }
    }
}
