using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Commands;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Characters.Systems;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters
{
    // TODO: why does this not implement ServerCommandHandler like the ServerPlayerCommandHandler

    public class EnemyCommandHandler : MonoBehaviour, ICommandable
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

        public void OnCancelInteract(IEventCollectable ec) { }

        public void OnDialogNext(IEventCollectable ec) { }

        public void OnEquipItem(IEventCollectable ec, int slotIndex) { }

        public void OnDropItem(
            IEventCollectable ec,
            StorageType type,
            int slotIndex,
            string itemId
        ) { }

        public void OnPurchase(IEventCollectable ec, uint netId, string productId, int amount) { }

        public void OnQuestAccepted(IEventCollectable ec, string questId) { }

        public void OnQuestDecided(IEventCollectable ec) { }

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
            int goldAmount,
            System.Action<bool> onComplete
        ) { }

        public void OnSetUserId(IEventCollectable ec, string tokenId) { }

        public void OnTeleportAccepted(IEventCollectable ec, string portalId, int zoneId)
        {
            throw new System.NotImplementedException();
        }
        public void OnSendMessage(IEventCollectable ec, string message) { }
    }
}
