using System.Collections.Generic;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems
{
    /// <summary>
    /// LootItem represents an item in the game world that can be interacted with by players.
    /// It is responsible for sending pickup commands to the server when a player interacts with it.
    /// </summary>
    public class InventorySystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private ServerConfig config;
        private uint netId;
        private InventoryStateStorage inventoryState;
        private int inventorySize = 20;
        private string[] inventorySlots = new string[20];

        private void InitEmptyInventory()
        {
            for (int i = 0; i < inventorySize; i++)
            {
                inventorySlots[i] = string.Empty;
            }
        }

        public void Initialize(uint netId, InventoryStateStorage inventoryState)
        {
            this.netId = netId;
            this.inventoryState = inventoryState;
            logger.Log($"Initializing InventorySystem for player {netId}", this);
            inventorySize = config.InventorySize > 0 ? config.InventorySize : 20;

            InitEmptyInventory();
        }

        public void OnPickUp(IEventCollectable ec, string itemId, System.Action<bool> onComplete)
        {
            logger.Log($"Attempting to pick up item {itemId} for player {netId}", this);

            for (int i = 0; i < inventorySize; i++)
            {
                if (string.IsNullOrEmpty(inventorySlots[i]))
                {
                    inventorySlots[i] = itemId;
                    logger.Log($"Item {itemId} added to inventory at slot {i}", this);
                    inventoryState.AddItem(itemId, i);
                    onComplete(true);
                    return;
                }
            }

            logger.Log($"Inventory full, cannot pick up item {itemId}", this);
            onComplete(false);
        }

        public void OnMoveItem(IEventCollectable ec, int sourceSlot, int targetSlot)
        {
            if (sourceSlot < 0 || sourceSlot >= inventorySize)
                return;
            if (targetSlot < 0 || targetSlot >= inventorySize)
                return;

            logger.Log(
                $"Swapping item for player {netId} from slot {sourceSlot} to {targetSlot}",
                this
            );

            string sourceItemId = inventorySlots[sourceSlot];
            string targetItemId = inventorySlots[targetSlot];

            inventorySlots[targetSlot] = sourceItemId;
            inventorySlots[sourceSlot] = targetItemId;

            inventoryState.SwapItems(sourceSlot, targetSlot);
        }

        public bool TryRemoveItemAt(int slotIndex, out string itemId)
        {
            itemId = null;

            if (slotIndex < 0 || slotIndex >= inventorySize)
                return false;

            itemId = inventorySlots[slotIndex];
            if (string.IsNullOrEmpty(itemId))
                return false;

            inventorySlots[slotIndex] = string.Empty;
            inventoryState.DropItem(slotIndex);
            return true;
        }

        public bool TryAddItemAt(int slotIndex, string itemId)
        {
            if (slotIndex < 0 || slotIndex >= inventorySize)
                return false;
            if (string.IsNullOrEmpty(itemId))
                return false;
            if (!string.IsNullOrEmpty(inventorySlots[slotIndex]))
                return false;

            inventorySlots[slotIndex] = itemId;
            inventoryState.AddItem(itemId, slotIndex);
            return true;
        }

        public bool TryGetItemAt(int slotIndex, out string itemId)
        {
            itemId = null;

            if (slotIndex < 0 || slotIndex >= inventorySize)
                return false;

            itemId = inventorySlots[slotIndex];
            return !string.IsNullOrEmpty(itemId);
        }

        public bool TryReplaceItemAt(int slotIndex, string itemId)
        {
            if (slotIndex < 0 || slotIndex >= inventorySize)
                return false;
            if (string.IsNullOrEmpty(itemId))
                return false;

            inventorySlots[slotIndex] = itemId;
            inventoryState.AddItem(itemId, slotIndex);
            return true;
        }

        public bool RemoveItemById(string itemId, out int removedSlot)
        {
            removedSlot = -1;

            if (string.IsNullOrEmpty(itemId))
                return false;

            for (int i = 0; i < inventorySize; i++)
            {
                if (inventorySlots[i] == itemId)
                {
                    inventorySlots[i] = string.Empty;
                    inventoryState.DropItem(i);
                    removedSlot = i;
                    logger.Log(
                        $"Removed item {itemId} from inventory slot {i} for player {netId}",
                        this
                    );
                    return true;
                }
            }

            return false;
        }

        public string OnDropItem(IEventCollectable ec, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= inventorySize)
                return null;

            string itemId = inventorySlots[slotIndex];
            if (string.IsNullOrEmpty(itemId))
                return null;

            logger.Log($"Dropping item {itemId} for player {netId} from slot {slotIndex}", this);

            inventorySlots[slotIndex] = string.Empty;
            inventoryState.DropItem(slotIndex);

            return itemId;
        }

        public void GameTick(float dt) { }

        public void LoadInventory(string[] inventoryData, string[] hotbarData)
        {
            // TODO: implement inventory loading logic, currently just logs the loaded inventory
            logger.Log($"Loaded inventory for player {netId}", this);
        }
    }
}
