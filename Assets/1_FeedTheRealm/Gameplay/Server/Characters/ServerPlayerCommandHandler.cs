using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Commands;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Characters.Systems;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters
{
    public class ServerPlayerCommandHandler : ServerCommandHandler
    {
        private InventorySystem inventorySystem;

        public void Initialize(
            MovementSystem movementSystem,
            DashSystem dashSystem,
            UseSystem useSystem,
            InteractSystem interactSystem,
            InventorySystem inventorySystem
        )
        {
            Initialize(movementSystem, dashSystem, useSystem, interactSystem);
            this.inventorySystem = inventorySystem;
        }

        public override void OnMoveItem(
            IEventCollectable ec,
            StorageType sourceType,
            int sourceSlot,
            StorageType targetType,
            int targetSlot
        )
        {
            inventorySystem.OnMoveItem(ec, sourceType, sourceSlot, targetType, targetSlot);
        }

        public override void OnMove(IEventCollectable ec, Vector3 direction)
        {
            movementSystem.OnMove(direction);
        }

        public override void OnDropItem(
            IEventCollectable ec,
            StorageType storageType,
            int slotIndex,
            string itemId
        )
        {
            inventorySystem.OnDropItem(ec, slotIndex, storageType);
        }

        public override void OnPickUp(
            IEventCollectable ec,
            string itemId,
            System.Action<bool> onComplete
        )
        {
            inventorySystem.OnPickUp(ec, itemId, onComplete);
        }
    }
}
