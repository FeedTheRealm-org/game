using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Commands;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Characters.Systems;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters
{
    public class NpcCommandHandler : MonoBehaviour, ICommandable
    {
        private MovementSystem movementSystem;

        public void Initialize(MovementSystem movementSystem)
        {
            this.movementSystem = movementSystem;
        }

        public void OnMove(IEventCollectable ec, Vector3 direction)
        {
            movementSystem.OnMove(direction);
        }

        public void OnDash(IEventCollectable ec, Vector3 direction) { }

        public void OnUse(IEventCollectable ec) { }

        public void OnInteract(IEventCollectable ec) { }

        public void OnDialogNext(IEventCollectable ec) { }

        public void OnEquipItem(IEventCollectable ec, int slotIndex) { }

        public void OnDropItem(
            IEventCollectable ec,
            StorageType type,
            int slotIndex,
            string itemId
        ) { }

        public void OnPurchase(IEventCollectable ec, string itemId, int amount) { }

        public void OnQuestAccepted(IEventCollectable ec) { }

        public void OnMoveItem(
            IEventCollectable ec,
            StorageType sourceType,
            int sourceSlot,
            StorageType targetType,
            int targetSlot
        ) { }

        public void OnPickUp(
            IEventCollectable ec,
            string itemId,
            System.Action<bool> onComplete
        ) { }
    }
}
