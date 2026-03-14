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
        private DropItemSystem dropItemSystem;

        // TODO: Serialize field whatever possible
        public void Initialize(
            MovementSystem movementSystem,
            DashSystem dashSystem,
            UseSystem useSystem,
            InventorySystem inventorySystem,
            DropItemSystem dropItemSystem
        )
        {
            this.movementSystem = movementSystem;
            this.dashSystem = dashSystem;
            this.useSystem = useSystem;
            this.inventorySystem = inventorySystem;
            this.dropItemSystem = dropItemSystem;
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

        public void OnEquip(IEventCollectable ec) { }

        public void OnDropItem(IEventCollectable ec, int slotIndex)
        {
            string itemId = inventorySystem.OnDropItem(ec, slotIndex);
            if (!string.IsNullOrEmpty(itemId))
            {
                dropItemSystem.OnDropItem(itemId);
            }
        }

        public void OnPurchase(IEventCollectable ec) { }

        public void OnQuestAccepted(IEventCollectable ec) { }

        public void OnMoveItem(IEventCollectable ec, int sourceSlot, int targetSlot)
        {
            inventorySystem.OnMoveItem(ec, sourceSlot, targetSlot);
        }

        public void OnPickUp(IEventCollectable ec, string itemId, System.Action<bool> onComplete)
        {
            inventorySystem.OnPickUp(ec, itemId, onComplete);
        }
    }
}
