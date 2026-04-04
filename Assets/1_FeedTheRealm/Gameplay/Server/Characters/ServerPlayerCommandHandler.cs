using System.Threading.Tasks;
using API;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Characters;
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
        private CharacterStateStorage stateStorage;
        private PlayerService playerService;
        private string serverAccessToken;
        private bool isResolvingCharacterId;

        public void Initialize(
            MovementSystem movementSystem,
            DashSystem dashSystem,
            UseSystem useSystem,
            PlayerInteractSystem interactSystem,
            InventorySystem inventorySystem,
            QuestSystem questSystem,
            CharacterStateStorage stateStorage,
            PlayerService playerService,
            string serverAccessToken
        )
        {
            this.movementSystem = movementSystem;
            this.dashSystem = dashSystem;
            this.useSystem = useSystem;
            this.interactSystem = interactSystem;
            this.inventorySystem = inventorySystem;
            this.questSystem = questSystem;
            this.stateStorage = stateStorage;
            this.playerService = playerService;
            this.serverAccessToken = serverAccessToken;
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

        public override void OnSetUserId(IEventCollectable ec, string tokenId)
        {
            if (isResolvingCharacterId)
                return;

            if (string.IsNullOrWhiteSpace(tokenId))
            {
                Debug.LogWarning(
                    "[ServerPlayerCommandHandler] Received empty world join token.",
                    this
                );
                return;
            }

            _ = ResolveAndSetUserIdFromTokenAsync(tokenId);
        }

        private async Task ResolveAndSetUserIdFromTokenAsync(string tokenId)
        {
            isResolvingCharacterId = true;
            try
            {
                var consumeResponse = await playerService.ConsumeWorldJoinTokenAsync(
                    tokenId,
                    serverAccessToken
                );
                if (consumeResponse == null || string.IsNullOrWhiteSpace(consumeResponse.user_id))
                    return;

                if (!System.Guid.TryParse(consumeResponse.user_id, out _))
                {
                    Debug.LogWarning(
                        $"[ServerPlayerCommandHandler] Invalid user_id returned from consume endpoint: '{consumeResponse.user_id}'.",
                        this
                    );
                    return;
                }

                stateStorage.SetCharacterId(consumeResponse.user_id);
            }
            finally
            {
                isResolvingCharacterId = false;
            }
        }
    }
}
