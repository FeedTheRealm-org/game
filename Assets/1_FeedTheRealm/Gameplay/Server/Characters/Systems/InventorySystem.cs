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
        private int hotbarSize = 5;
        private string[] inventorySlots = new string[20];
        private string[] hotbarSlots = new string[5];
        private Dictionary<string, bool> itemsDropped = new Dictionary<string, bool>();

        private void InitEmptyInventory()
        {
            for (int i = 0; i < inventorySize; i++)
            {
                inventorySlots[i] = string.Empty;
            }

            for (int i = 0; i < hotbarSize; i++)
            {
                hotbarSlots[i] = string.Empty;
            }
        }

        public void Initialize(uint netId, InventoryStateStorage inventoryState)
        {
            this.netId = netId;
            this.inventoryState = inventoryState;
            logger.Log($"Initializing InventorySystem for player {netId}", this);
            inventorySize = config.InventorySize > 0 ? config.InventorySize : 20;
            hotbarSize = config.HotbarSize > 0 ? config.HotbarSize : 5;

            InitEmptyInventory();
        }

        public void OnPickUp(IEventCollectable ec, string itemId, System.Action<bool> onComplete)
        {
            if (itemId.StartsWith("Dropped-Item"))
            {
                if (!itemsDropped.ContainsKey(itemId) || !itemsDropped[itemId])
                {
                    itemsDropped[itemId] = true;
                    return;
                }
                else
                {
                    itemsDropped.Remove(itemId);
                }
            }

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
    }
}
