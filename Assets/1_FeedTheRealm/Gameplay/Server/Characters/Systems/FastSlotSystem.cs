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
    public class FastSlotSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private ServerConfig config;
        private uint netId;
        private FastSlotStateStorage fastSlotState;
        private int fastSlotSize = 5;
        private string[] fastSlotSlots = new string[5];

        private void InitEmptyInventory()
        {
            for (int i = 0; i < fastSlotSize; i++)
            {
                fastSlotSlots[i] = string.Empty;
            }
        }

        public void Initialize(uint netId, FastSlotStateStorage fastSlotState)
        {
            this.netId = netId;
            this.fastSlotState = fastSlotState;
            logger.Log($"Initializing FastSlotSystem for player {netId}", this);
            fastSlotSize = config.FastSlotSize > 0 ? config.FastSlotSize : 5;

            InitEmptyInventory();
        }

        public void OnMoveItem(IEventCollectable ec, int sourceSlot, int targetSlot)
        {
            if (sourceSlot < 0 || sourceSlot >= fastSlotSize)
                return;
            if (targetSlot < 0 || targetSlot >= fastSlotSize)
                return;

            logger.Log(
                $"Swapping item for player {netId} from slot {sourceSlot} to {targetSlot}",
                this
            );

            string sourceItemId = fastSlotSlots[sourceSlot];
            string targetItemId = fastSlotSlots[targetSlot];

            fastSlotSlots[targetSlot] = sourceItemId;
            fastSlotSlots[sourceSlot] = targetItemId;

            fastSlotState.SwapItems(sourceSlot, targetSlot);
        }

        public bool TryRemoveItemAt(int slotIndex, out string itemId)
        {
            itemId = null;

            if (slotIndex < 0 || slotIndex >= fastSlotSize)
                return false;

            itemId = fastSlotSlots[slotIndex];
            if (string.IsNullOrEmpty(itemId))
                return false;

            fastSlotSlots[slotIndex] = string.Empty;
            fastSlotState.DropItem(slotIndex);
            return true;
        }

        public bool TryAddItemAt(int slotIndex, string itemId)
        {
            if (slotIndex < 0 || slotIndex >= fastSlotSize)
                return false;
            if (string.IsNullOrEmpty(itemId))
                return false;
            if (!string.IsNullOrEmpty(fastSlotSlots[slotIndex]))
                return false;

            fastSlotSlots[slotIndex] = itemId;
            fastSlotState.AddItem(itemId, slotIndex);
            return true;
        }

        public bool TryGetItemAt(int slotIndex, out string itemId)
        {
            itemId = null;

            if (slotIndex < 0 || slotIndex >= fastSlotSize)
                return false;

            itemId = fastSlotSlots[slotIndex];
            return !string.IsNullOrEmpty(itemId);
        }

        public bool TryReplaceItemAt(int slotIndex, string itemId)
        {
            if (slotIndex < 0 || slotIndex >= fastSlotSize)
                return false;
            if (string.IsNullOrEmpty(itemId))
                return false;

            fastSlotSlots[slotIndex] = itemId;
            fastSlotState.AddItem(itemId, slotIndex);
            return true;
        }

        public bool RemoveItemById(string itemId, out int removedSlot)
        {
            removedSlot = -1;

            if (string.IsNullOrEmpty(itemId))
                return false;

            for (int i = 0; i < fastSlotSize; i++)
            {
                if (fastSlotSlots[i] == itemId)
                {
                    fastSlotSlots[i] = string.Empty;
                    fastSlotState.DropItem(i);
                    removedSlot = i;
                    logger.Log(
                        $"Unequipped item {itemId} from fast slot {i} for player {netId}",
                        this
                    );
                    return true;
                }
            }

            return false;
        }

        public bool ContainsItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return false;

            for (int i = 0; i < fastSlotSize; i++)
            {
                if (fastSlotSlots[i] == itemId)
                    return true;
            }

            return false;
        }

        public string OnDropItem(IEventCollectable ec, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= fastSlotSize)
                return null;

            string itemId = fastSlotSlots[slotIndex];
            if (string.IsNullOrEmpty(itemId))
                return null;

            logger.Log($"Dropping item {itemId} for player {netId} from slot {slotIndex}", this);

            fastSlotSlots[slotIndex] = string.Empty;
            fastSlotState.DropItem(slotIndex);

            return itemId;
        }

        public void GameTick(float dt) { }
    }
}
