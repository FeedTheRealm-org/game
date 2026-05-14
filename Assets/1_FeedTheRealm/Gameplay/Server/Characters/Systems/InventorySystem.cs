using System;
using Amazon.Runtime;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Core.Server.Persistence.Schemas;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using FTR.Gameplay.Server.Registry;
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
        private CharacterStateStorage characterState;
        private WorldMonitor world;
        private UseSystem useSystem;

        private int activeSlot = 0;

        private InventoryItemModel[] inventorySlots;
        private InventoryItemModel[] fastSlots;

        public InventoryItemModel[] GetCurrentInventory() => inventorySlots;

        public InventoryItemModel[] GetCurrentFastAccess() => fastSlots;

        public void Initialize(
            uint netId,
            InventoryStateStorage inventoryState,
            CharacterStateStorage characterState,
            WorldMonitor world
        )
        {
            this.netId = netId;
            this.inventoryState = inventoryState;
            this.characterState = characterState;
            this.world = world;
            SubscribeToQuestReward();
            useSystem = transform.root.GetComponentInChildren<UseSystem>();
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
            {
                logger.Log($"Picked up item {itemId} for player {netId}", this);
                var connId = GetPlayerConnectionId(netId);
                world.Events.Enqueue(new LootbagPickedUpEvent(netId, connId.Value));
            }
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

            var connId = GetPlayerConnectionId(netId);

            if (!CanAddItemsToInventory(itemId, amount))
            {
                logger.Log(
                    $"[InventorySystem] Inventory has insufficient space for {amount}x {itemId}",
                    this
                );

                world.Events.Enqueue(
                    new InventoryErrorEvent(
                        netId,
                        new InventoryErrorContent { ErrorType = InventoryErrorType.NotEnoughSpace },
                        connId.Value
                    )
                );
                return false;
            }

            bool added = AddItemToInventory(itemId, amount);

            if (!added)
            {
                logger.Log(
                    $"[InventorySystem] Failed to add purchased items despite space check for Player:{netId}",
                    this
                );
                return false;
            }

            world.Events.Enqueue(new ShopPurchaseConfirmEvent(netId, connId.Value));
            logger.Log(
                $"[InventorySystem] Purchased item {itemId} x{amount} added to inventory for Player:{netId}",
                this
            );
            return true;
        }

        public void OnMoveItem(
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

            var src = GetStorage(sourceType);
            var tgt = GetStorage(targetType);

            var from = new SlotRef(sourceType, sourceSlot, src[sourceSlot]);
            var to = new SlotRef(targetType, targetSlot, tgt[targetSlot]);

            if (TryStack(from, to))
                return;

            PerformSwap(from, to);
        }

        // ── Stacking ─────────────────────────────────────────────────────────────

        private bool TryStack(SlotRef from, SlotRef to)
        {
            if (!CanStack(from, to))
                return false;

            int maxStack = GetMaxStack(from.Item.ItemId);
            int toMove = Mathf.Min(maxStack - to.Item.Quantity, from.Item.Quantity);

            to.Item.Quantity += toMove;
            GetStorage(to.Type)[to.Index] = to.Item;

            from.Item.Quantity -= toMove;
            if (from.Item.Quantity <= 0)
            {
                from.Item.ItemId = string.Empty;
                from.Item.Quantity = 0;
            }
            GetStorage(from.Type)[from.Index] = from.Item;

            logger.Log(
                $"Stacked Player:{netId}: moved {toMove}x {to.Item.ItemId} "
                    + $"from {from.Type}[{from.Index}] → {to.Type}[{to.Index}] "
                    + $"(now {to.Item.Quantity} in target)",
                this
            );

            inventoryState.AddItem(to.Type, to.Index, to.Item.ItemId, to.Item.Quantity);

            if (string.IsNullOrEmpty(from.Item.ItemId))
                inventoryState.DropItem(from.Type, from.Index);
            else
                inventoryState.AddItem(from.Type, from.Index, from.Item.ItemId, from.Item.Quantity);

            if (from.IsActiveSlot(activeSlot))
            {
                characterState.SetEquippedItemId(fastSlots[activeSlot].ItemId);
                useSystem?.EquipItem((fastSlots[activeSlot].ItemId, activeSlot));
            }

            return true;
        }

        private bool CanStack(SlotRef from, SlotRef to)
        {
            if (string.IsNullOrEmpty(from.Item.ItemId))
                return false;
            if (from.Item.ItemId != to.Item.ItemId)
                return false;
            if (from.Type == to.Type && from.Index == to.Index)
                return false;

            int maxStack = GetMaxStack(from.Item.ItemId);
            return to.Item.Quantity < maxStack;
        }

        // ── Swap ─────────────────────────────────────────────────────────────────

        private void PerformSwap(SlotRef from, SlotRef to)
        {
            var src = GetStorage(from.Type);
            var tgt = GetStorage(to.Type);

            src[from.Index] = to.Item;
            tgt[to.Index] = from.Item;

            logger.Log(
                $"Swapped Player:{netId}: "
                    + $"{from.Type}[{from.Index}]({from.Item.ItemId}) <-> "
                    + $"{to.Type}[{to.Index}]({to.Item.ItemId})",
                this
            );

            inventoryState.SwapItems(
                from.Type,
                from.Index,
                from.Item.ItemId,
                from.Item.Quantity,
                to.Type,
                to.Index,
                to.Item.ItemId,
                to.Item.Quantity
            );

            if (from.IsActiveSlot(activeSlot) || to.IsActiveSlot(activeSlot))
            {
                characterState.SetEquippedItemId(fastSlots[activeSlot].ItemId);
                useSystem?.EquipItem((fastSlots[activeSlot].ItemId, activeSlot));
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

            InventoryItemModel[] storage = GetStorage(storageType);
            var itemId = storage[slotIndex].ItemId;
            if (string.IsNullOrEmpty(itemId))
                return null;

            storage[slotIndex].ItemId = string.Empty;
            storage[slotIndex].Quantity = 0;
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

            var itemId = fastSlots[slotIndex].ItemId;
            characterState.SetEquippedItemId(itemId);
            Debug.Log($"Player:{netId} equipped item {itemId} from fast slot {slotIndex}");

            useSystem?.EquipItem((itemId, slotIndex));
        }

        public void LoadInventory(
            InventoryItemModel[] inventoryData,
            InventoryItemModel[] fastSlotData
        )
        {
            this.inventorySlots = inventoryData;
            this.fastSlots = fastSlotData;

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

        private bool CanAddItemsToInventory(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0)
                return false;

            int maxStack = GetMaxStack(itemId);
            int remaining = amount;

            for (int i = 0; i < config.InventorySize && remaining > 0; i++)
            {
                var slot = inventorySlots[i];

                if (string.IsNullOrEmpty(slot.ItemId))
                {
                    remaining -= maxStack;
                }
                else if (slot.ItemId == itemId)
                {
                    int space = maxStack - slot.Quantity;
                    if (space > 0)
                        remaining -= space;
                }
            }

            return remaining <= 0;
        }

        private bool AddItemToInventory(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId))
                return false;

            int maxStack = GetMaxStack(itemId);
            int remaining = quantity;

            for (int i = 0; i < config.InventorySize && remaining > 0; i++)
            {
                var slot = inventorySlots[i];
                if (slot.ItemId != itemId)
                    continue;

                int space = maxStack - slot.Quantity;
                if (space <= 0)
                    continue;

                int toAdd = Mathf.Min(space, remaining);
                slot.Quantity += toAdd;
                remaining -= toAdd;
                inventoryState.AddItem(StorageType.Inventory, i, itemId, slot.Quantity);
            }

            for (int i = 0; i < config.InventorySize && remaining > 0; i++)
            {
                var slot = inventorySlots[i];
                if (!string.IsNullOrEmpty(slot.ItemId))
                    continue;

                int toAdd = Mathf.Min(maxStack, remaining);
                slot.ItemId = itemId;
                slot.Quantity = toAdd;
                remaining -= toAdd;
                inventoryState.AddItem(StorageType.Inventory, i, itemId, slot.Quantity);
            }

            return remaining < quantity;
        }

        private int GetMaxStack(string itemId)
        {
            var data = ServerItemsRegistry.GetItemById(itemId);
            int maxStack = data?.maxStack ?? 1;
            return maxStack > 0 ? maxStack : 1;
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

        public void ConsumeItem(string itemId)
        {
            string equippedItemId = fastSlots[activeSlot].ItemId;
            if (equippedItemId == itemId)
            {
                logger?.Log(
                    $"[InventorySystem] Consuming item '{itemId}' from fast slot {activeSlot}. Remaining quantity: {fastSlots[activeSlot].Quantity - 1}",
                    this
                );

                if (fastSlots[activeSlot].Quantity > 1)
                {
                    fastSlots[activeSlot].Quantity--;
                    inventoryState.AddItem(
                        StorageType.FastSlot,
                        activeSlot,
                        fastSlots[activeSlot].ItemId,
                        fastSlots[activeSlot].Quantity
                    );
                }
                else
                {
                    fastSlots[activeSlot].ItemId = string.Empty;
                    fastSlots[activeSlot].Quantity = 0;
                    inventoryState.DropItem(StorageType.FastSlot, activeSlot);
                    characterState.SetEquippedItemId(string.Empty);

                    useSystem?.EquipItem((string.Empty, activeSlot));
                }
            }
        }

        private struct SlotRef
        {
            public StorageType Type;
            public int Index;
            public InventoryItemModel Item;

            public SlotRef(StorageType type, int index, InventoryItemModel item)
            {
                Type = type;
                Index = index;
                Item = item;
            }

            public bool IsActiveSlot(int active) => Type == StorageType.FastSlot && Index == active;
        }

        private int? GetPlayerConnectionId(uint playerNetId)
        {
            if (world.Entities.TryGet(playerNetId, out var entity) && entity.ConnectionId.HasValue)
                return entity.ConnectionId.Value;

            return null;
        }
    }
}
