using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Characters.Systems;
using FTR.Gameplay.Server.Registry;
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
        private GoldSystem goldSystem;

        public void Initialize(
            MovementSystem movementSystem,
            DashSystem dashSystem,
            UseSystem useSystem,
            PlayerInteractSystem interactSystem,
            InventorySystem inventorySystem,
            QuestSystem questSystem,
            GoldSystem goldSystem,
            WorldMonitor worldMonitor,
            uint netId,
            uint ownNetId
        )
        {
            this.movementSystem = movementSystem;
            this.dashSystem = dashSystem;
            this.useSystem = useSystem;
            this.interactSystem = interactSystem;
            this.inventorySystem = inventorySystem;
            this.questSystem = questSystem;
            this.goldSystem = goldSystem;

            questSystem.Initialize(netId, worldMonitor, ownNetId);
            interactSystem.Initialize(netId, worldMonitor, ownNetId);
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
            interactSystem.TryInteract(ec);
        }

        public override void OnDialogNext(IEventCollectable ec)
        {
            interactSystem.TryContinue(ec);
        }

        public override void OnQuestAccepted(IEventCollectable ec, string questId)
        {
            questSystem.OnQuestAccepted(ec, questId);
            interactSystem.NotifyQuestDecided();
        }

        public override void OnQuestDecided(IEventCollectable ec)
        {
            interactSystem.NotifyQuestDecided();
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

        public override void OnPurchase(
            IEventCollectable ec,
            uint netId,
            string productId,
            int amount
        )
        {
            if (amount <= 0)
            {
                Debug.LogWarning(
                    $"[ServerPlayerCommandHandler] Invalid purchase amount {amount} for product {productId}"
                );
                return;
            }

            var product = ServerShopRegistry.GetProductById(productId);
            if (product == null)
            {
                Debug.LogWarning($"[ServerPlayerCommandHandler] Product not found: {productId}");
                return;
            }

            if (goldSystem.HasEnoughGold(netId, productId, product.price, amount))
            {
                Debug.Log(
                    $"[ServerPlayerCommandHandler] Processing purchase of {productId} x{amount}"
                );
                goldSystem.ReduceGold(ec, product.price * amount);
                inventorySystem.OnPurchase(ec, productId, amount);
            }
        }
    }
}
