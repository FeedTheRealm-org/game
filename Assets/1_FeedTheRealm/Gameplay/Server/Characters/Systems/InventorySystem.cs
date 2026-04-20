using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Core.Server.Persistence;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class InventorySystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private ServerConfig config;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            if (resolver.TryResolve<QuestRewardItemEvent>(out var ev) && ev != null)
                questRewardItemEvent = ev;
            if (resolver.TryResolve<ItemEquippedEvent>(out var equipEv) && equipEv != null)
                itemEquippedEvent = equipEv;
        }

        private QuestRewardItemEvent questRewardItemEvent;
        private ItemEquippedEvent itemEquippedEvent;
        private bool subscribedToQuestReward = false;

        private uint netId;
        private InventoryStateStorage inventoryState;
        private CharacterStateStorage characterState;

        private int inventorySize = 12;
        private int fastSlotSize = 5;
        private int activeSlot = 0;

        private string[] inventorySlots;
        private string[] fastSlots;

        public void Initialize(
            uint netId,
            InventoryStateStorage inventoryState,
            CharacterStateStorage characterState
        )
        {
            this.netId = netId;
            this.inventoryState = inventoryState;
            this.characterState = characterState;

            inventorySize = config.InventorySize > 0 ? config.InventorySize : 12;
            fastSlotSize = config.FastSlotSize > 0 ? config.FastSlotSize : 5;

            inventorySlots = new string[inventorySize];
            fastSlots = new string[fastSlotSize];

            for (int i = 0; i < inventorySize; i++)
                inventorySlots[i] = string.Empty;
            for (int i = 0; i < fastSlotSize; i++)
                fastSlots[i] = string.Empty;

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

            int emptySlots = 0;
            for (int i = 0; i < inventorySize; i++)
                if (string.IsNullOrEmpty(inventorySlots[i]))
                    emptySlots++;

            if (emptySlots < amount)
            {
                logger.Log(
                    $"Inventory has insufficient space for purchased item {itemId} x{amount} (free={emptySlots})",
                    this
                );
                return false;
            }

            for (int i = 0; i < amount; i++)
                AddItemToInventory(itemId);

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

            string[] src = GetStorage(sourceType);
            string[] tgt = GetStorage(targetType);

            string sourceItemId = src[sourceSlot];
            string targetItemId = tgt[targetSlot];

            src[sourceSlot] = targetItemId;
            tgt[targetSlot] = sourceItemId;

            logger.Log(
                $"Swapped Player:{netId}: "
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

            if (sourceType == StorageType.FastSlot && sourceSlot == activeSlot)
            {
                characterState.SetEquippedItemId(fastSlots[activeSlot]);
            }
            else if (targetType == StorageType.FastSlot && targetSlot == activeSlot)
            {
                characterState.SetEquippedItemId(fastSlots[activeSlot]);
            }
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
                $"Dropped item {itemId} from {storageType}[{slotIndex}] for Player:{netId}",
                this
            );

            if (prefabProvider != null)
                SpawnItem(dropPosition, prefabProvider, itemId);

            if (storageType == StorageType.FastSlot && slotIndex == activeSlot)
            {
                characterState.SetEquippedItemId(string.Empty);
            }

            return itemId;
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

            var itemId = fastSlots[slotIndex];
            characterState.SetEquippedItemId(itemId);
            Debug.Log($"Player:{netId} equipped item {itemId} from fast slot {slotIndex}");

            // slotIndex is now included so UseSystem can maintain per-slot cooldowns
            itemEquippedEvent.Raise((netId, itemId, slotIndex));
        }

        public void LoadInventory(string[] inventoryData, string[] fastSlotData)
        {
            // TODO: populate inventorySlots / fastSlots from saved data
            logger.Log($"Loaded inventory for Player:{netId}", this);
        }

        public void GameTick(float dt) { }

        // ── Private helpers ───────────────────────────────────────────────────

        private bool AddItemToInventory(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return false;

            for (int i = 0; i < inventorySize; i++)
            {
                if (!string.IsNullOrEmpty(inventorySlots[i]))
                    continue;

                inventorySlots[i] = itemId;
                inventoryState.AddItem(StorageType.Inventory, i, itemId);
                return true;
            }

            return false;
        }

        private string[] GetStorage(StorageType type) =>
            type == StorageType.FastSlot ? fastSlots : inventorySlots;

        private int GetStorageSize(StorageType type) =>
            type == StorageType.FastSlot ? fastSlotSize : inventorySize;

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

            var lootInstance = Object.Instantiate(lootPrefab, dropPosition, Quaternion.identity);
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

        private void ConsumeItem(string itemId)
        {
            string equippedItemId = fastSlots[activeSlot];
            if (equippedItemId == data.itemId)
            {
                logger?.Log(
                    $"[InventorySystem] Consuming item '{data.itemId}' from fast slot {activeSlot}.",
                    this
                );
                fastSlots[activeSlot] = string.Empty;
                inventoryState.DropItem(StorageType.FastSlot, activeSlot);
                characterState.SetEquippedItemId(string.Empty);
            }
        }
    }
}
