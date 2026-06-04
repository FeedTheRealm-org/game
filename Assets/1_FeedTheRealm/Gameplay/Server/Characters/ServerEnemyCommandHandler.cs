using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Characters.Systems;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters
{
    public class EnemyCommandHandler : ServerCommandHandler
    {
        private MovementSystem movementSystem;
        private DashSystem dashSystem;
        private UseSystem useSystem;

        public void Initialize(
            MovementSystem movementSystem,
            DashSystem dashSystem,
            UseSystem useSystem
        )
        {
            this.movementSystem = movementSystem;
            this.dashSystem = dashSystem;
            this.useSystem = useSystem;
        }

        public override void OnMove(IEventCollectable ec, Vector3 direction)
        {
            movementSystem.OnMove(direction);
        }

        public override void OnDash(IEventCollectable ec, Vector3 direction)
        {
            dashSystem.OnDash(ec, direction);
        }

        public override void OnUse(IEventCollectable ec, Vector3 direction)
        {
            useSystem.OnUse(ec, direction);
        }

        public override void OnInteract(IEventCollectable ec) { }

        public override void OnDialogNext(IEventCollectable ec) { }

        public override void OnEquipItem(IEventCollectable ec, int slotIndex) { }

        public override void OnDropItem(
            IEventCollectable ec,
            StorageType type,
            int slotIndex,
            string itemId
        ) { }

        public override void OnPurchase(
            IEventCollectable ec,
            uint netId,
            string productId,
            int amount,
            string shopId
        ) { }

        public override void OnQuestAccepted(IEventCollectable ec, string questId) { }

        public override void OnQuestRejected(IEventCollectable ec) { }

        public override void OnMoveItem(
            IEventCollectable ec,
            StorageType sourceType,
            int sourceSlot,
            StorageType targetType,
            int targetSlot
        ) { }

        public override void OnPickUp(
            IEventCollectable ec,
            string itemId,
            int goldAmount,
            System.Action<bool> onComplete
        ) { }

        public override void OnSetUserId(
            IEventCollectable ec,
            string tokenId,
            bool isTeleporting
        ) { }

        public override void OnSendMessage(IEventCollectable ec, string message) { }

        public override void OnTeleportAccepted(IEventCollectable ec, string portalId) { }
    }
}
