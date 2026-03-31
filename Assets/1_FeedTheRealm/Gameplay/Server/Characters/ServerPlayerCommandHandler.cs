using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Characters.Systems;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters
{
    public class ServerPlayerCommandHandler : ServerCommandHandler
    {
        private MovementSystem movementSystem;
        private DashSystem dashSystem;
        private UseSystem useSystem;
        private PlayerInteractSystem interactSystem;
        private InventorySystem inventorySystem;
        private QuestSystem questSystem;

        public void Initialize(
            MovementSystem movementSystem,
            DashSystem dashSystem,
            UseSystem useSystem,
            PlayerInteractSystem interactSystem,
            InventorySystem inventorySystem,
            QuestSystem questSystem
        )
        {
            this.movementSystem = movementSystem;
            this.dashSystem = dashSystem;
            this.useSystem = useSystem;
            this.interactSystem = interactSystem;
            this.inventorySystem = inventorySystem;
            this.questSystem = questSystem;
        }

        public override void OnMove(IEventCollectable ec, Vector3 direction)
        {
            movementSystem.OnMove(direction);
        }

        public override void OnDash(IEventCollectable ec, Vector3 direction)
        {
            dashSystem.OnDash(ec, direction);
        }

        public override void OnUse(IEventCollectable ec)
        {
            useSystem.OnUse(ec);
        }

        public override void OnInteract(IEventCollectable ec)
        {
            interactSystem.OnInteract(ec);
        }

        public override void OnDialogNext(IEventCollectable ec)
        {
            interactSystem.OnDialogNext(ec);
        }

        public override void OnQuestAccepted(IEventCollectable ec, string questId)
        {
            questSystem.OnQuestAccepted(ec, questId);
            interactSystem.OnQuestDecided();
        }

        public override void OnQuestDecided(IEventCollectable ec)
        {
            interactSystem.OnQuestDecided();
        }

        public override void OnEquipItem(IEventCollectable ec, int slotIndex)
        {
            inventorySystem.OnEquipItem(ec, slotIndex);
        }

        public override void OnDropItem(
            IEventCollectable ec,
            StorageType type,
            int slotIndex,
            string itemId
        )
        {
            Vector3 dropPosition = transform.position + transform.forward * 1.5f;
            inventorySystem.OnDropItem(ec, slotIndex, type, dropPosition, null);
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
