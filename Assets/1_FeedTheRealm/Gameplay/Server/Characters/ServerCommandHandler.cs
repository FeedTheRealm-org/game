using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Commands;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Characters.Systems;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters
{
    public class ServerCommandHandler : MonoBehaviour, ICommandable
    {
        private MovementSystem movementSystem;
        private DashSystem dashSystem;
        private UseSystem useSystem;
        private InventorySystem inventorySystem;
        private FastSlotSystem fastSlotSystem;

        // TODO: Serialize field whatever possible
        public void Initialize(
            MovementSystem movementSystem,
            DashSystem dashSystem,
            UseSystem useSystem,
            InventorySystem inventorySystem,
            FastSlotSystem fastSlotSystem
        )
        {
            this.movementSystem = movementSystem;
            this.dashSystem = dashSystem;
            this.useSystem = useSystem;
            this.inventorySystem = inventorySystem;
            this.fastSlotSystem = fastSlotSystem;
        }

        public void OnMove(IEventCollectable ec, Vector3 direction)
        {
            movementSystem.OnMove(direction);
        }

        public void OnDash(IEventCollectable ec, Vector3 direction)
        {
            dashSystem.OnDash(ec, direction);
        }

        public void OnUse(IEventCollectable ec)
        {
            useSystem.OnUse(ec);
        }

        public void OnInteract(IEventCollectable ec) { }

        public void OnEquipItem(IEventCollectable ec, int sourceSlot, int targetSlot, string itemId)
        {
            if (sourceSlot < 0 || targetSlot < 0)
                return;

            if (!inventorySystem.TryGetItemAt(sourceSlot, out string movingItemId))
                return;

            if (fastSlotSystem.TryGetItemAt(targetSlot, out string replacedFastItemId))
            {
                if (!inventorySystem.TryReplaceItemAt(sourceSlot, replacedFastItemId))
                    return;

                if (!fastSlotSystem.TryReplaceItemAt(targetSlot, movingItemId))
                    inventorySystem.TryReplaceItemAt(sourceSlot, movingItemId);

                return;
            }

            if (!inventorySystem.TryRemoveItemAt(sourceSlot, out movingItemId))
                return;

            if (!fastSlotSystem.TryAddItemAt(targetSlot, movingItemId))
            {
                inventorySystem.TryAddItemAt(sourceSlot, movingItemId);
            }
        }

        public void OnUnequipItem(
            IEventCollectable ec,
            int sourceSlot,
            int targetSlot,
            string itemId
        )
        {
            if (sourceSlot < 0 || targetSlot < 0)
                return;

            if (!fastSlotSystem.TryGetItemAt(sourceSlot, out string movingItemId))
                return;

            if (inventorySystem.TryGetItemAt(targetSlot, out string replacedInventoryItemId))
            {
                if (!fastSlotSystem.TryReplaceItemAt(sourceSlot, replacedInventoryItemId))
                    return;

                if (!inventorySystem.TryReplaceItemAt(targetSlot, movingItemId))
                    fastSlotSystem.TryReplaceItemAt(sourceSlot, movingItemId);

                return;
            }

            if (!fastSlotSystem.TryRemoveItemAt(sourceSlot, out movingItemId))
                return;

            if (!inventorySystem.TryAddItemAt(targetSlot, movingItemId))
            {
                fastSlotSystem.TryAddItemAt(sourceSlot, movingItemId);
            }
        }

        public void OnDropItem(IEventCollectable ec, StorageType type, int slotIndex, string itemId)
        {
            switch (type)
            {
                case StorageType.Inventory:
                    inventorySystem.OnDropItem(ec, slotIndex);
                    break;
                case StorageType.FastSlot:
                    fastSlotSystem.OnDropItem(ec, slotIndex);
                    break;
                default:
                    Debug.LogWarning($"Unknown storage type {type} for move item command");
                    break;
            }
        }

        public void OnPurchase(IEventCollectable ec) { }

        public void OnQuestAccepted(IEventCollectable ec) { }

        public void OnMoveItem(
            IEventCollectable ec,
            StorageType type,
            int sourceSlot,
            int targetSlot
        )
        {
            switch (type)
            {
                case StorageType.Inventory:
                    inventorySystem.OnMoveItem(ec, sourceSlot, targetSlot);
                    break;
                case StorageType.FastSlot:
                    fastSlotSystem.OnMoveItem(ec, sourceSlot, targetSlot);
                    break;
                default:
                    Debug.LogWarning($"Unknown storage type {type} for move item command");
                    break;
            }
        }

        public void OnPickUp(IEventCollectable ec, string itemId, System.Action<bool> onComplete)
        {
            inventorySystem.OnPickUp(ec, itemId, onComplete);
        }
    }
}
