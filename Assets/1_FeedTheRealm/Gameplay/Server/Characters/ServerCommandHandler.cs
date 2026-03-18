using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Commands;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Characters.Systems;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters
{
    public class ServerCommandHandler : MonoBehaviour, ICommandable
    {
        protected MovementSystem movementSystem;
        protected DashSystem dashSystem;
        protected UseSystem useSystem;
        protected InteractSystem interactSystem;

        // TODO: Serialize field whatever possible
        public void Initialize(
            MovementSystem movementSystem,
            DashSystem dashSystem,
            UseSystem useSystem,
            InteractSystem interactSystem
        )
        {
            this.movementSystem = movementSystem;
            this.dashSystem = dashSystem;
            this.useSystem = useSystem;
            this.interactSystem = interactSystem;
        }

        public virtual void OnMove(IEventCollectable ec, Vector3 direction)
        {
            movementSystem.OnMove(direction);
        }

        public virtual void OnDash(IEventCollectable ec, Vector3 direction)
        {
            dashSystem.OnDash(ec, direction);
        }

        public virtual void OnUse(IEventCollectable ec)
        {
            useSystem.OnUse(ec);
        }

        public virtual void OnInteract(IEventCollectable ec)
        {
            interactSystem.OnInteract(ec);
        }

        public virtual void OnDialogNext(IEventCollectable ec)
        {
            interactSystem.OnDialogNext(ec);
        }

        public virtual void OnEquipItem(IEventCollectable ec, int slotIndex) { }

        public virtual void OnDropItem(
            IEventCollectable ec,
            StorageType type,
            int slotIndex,
            string itemId
        ) { }

        public void OnPurchase(IEventCollectable ec) { }

        public void OnQuestAccepted(IEventCollectable ec) { }

        public virtual void OnMoveItem(
            IEventCollectable ec,
            StorageType sourceType,
            int sourceSlot,
            StorageType targetType,
            int targetSlot
        ) { }

        public virtual void OnPickUp(
            IEventCollectable ec,
            string itemId,
            System.Action<bool> onComplete
        ) { }
    }
}
