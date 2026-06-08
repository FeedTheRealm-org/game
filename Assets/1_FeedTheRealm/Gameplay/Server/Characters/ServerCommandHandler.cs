using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Commands;
using FTR.Core.Server.Events;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters
{
    /// <summary>
    /// Abstract base providing default no-op implementations of ICommandable.
    /// Concrete handlers override only what they need.
    /// </summary>
    public abstract class ServerCommandHandler : MonoBehaviour, ICommandable
    {
        public virtual void OnMove(IEventCollectable ec, Vector3 direction) { }

        public virtual void OnDash(IEventCollectable ec, Vector3 direction) { }

        public virtual void OnUse(IEventCollectable ec, Vector3 direction) { }

        public virtual void OnInteract(IEventCollectable ec) { }

        public virtual void OnDialogNext(IEventCollectable ec) { }

        public virtual void OnEquipItem(IEventCollectable ec, int slotIndex) { }

        public virtual void OnDropItem(
            IEventCollectable ec,
            StorageType type,
            int slotIndex,
            string itemId
        ) { }

        public virtual void OnPurchase(
            IEventCollectable ec,
            uint netId,
            string productId,
            int amount,
            string shopId
        ) { }

        public virtual void OnQuestAccepted(IEventCollectable ec, string questId) { }

        public virtual void OnQuestRejected(IEventCollectable ec) { }

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
            int goldAmount,
            System.Action<bool> onComplete
        ) { }

        public virtual void OnSetUserId(
            IEventCollectable ec,
            string tokenId,
            bool isTeleporting
        ) { }

        public virtual void OnTeleportAccepted(IEventCollectable ec, string portalId) { }

        public virtual void OnSendMessage(IEventCollectable ec, string message) { }
    }
}
