using System.Threading.Tasks;
using API;
using FTR.Core.Common.Config;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using FTR.Gameplay.Server.Registry;
using UnityEngine;
using VContainer;

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
        private GoldSystem goldSystem;
        private TeleportSystem teleportSystem;
        private ChatSystem chatSystem;

        private Config config;
        private PlayerQuestDecisionEvent playerQuestDecisionEvent;

        [Inject]
        public void Construct(IObjectResolver resolver, Config config)
        {
            if (resolver.TryResolve<PlayerQuestDecisionEvent>(out var ev) && ev != null)
            {
                playerQuestDecisionEvent = ev;
            }
            this.config = config;
        }

        public void Initialize(
            MovementSystem movementSystem,
            DashSystem dashSystem,
            UseSystem useSystem,
            PlayerInteractSystem interactSystem,
            InventorySystem inventorySystem,
            QuestSystem questSystem,
            CharacterStateStorage stateStorage,
            PlayerService playerService,
            string serverAccessToken,
            GoldSystem goldSystem,
            TeleportSystem teleportSystem,
            ChatSystem chatSystem
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
            this.goldSystem = goldSystem;
            this.teleportSystem = teleportSystem;
            this.chatSystem = chatSystem;
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
            interactSystem.TryInteract(ec);
        }

        public override void OnQuestAccepted(IEventCollectable ec, string questId)
        {
            string npcId = (interactSystem.CurrentInteractable as NpcInteractSystem)?.NpcId;
            questSystem.OnQuestAccepted(ec, questId, npcId);
        }

        public override void OnQuestRejected(IEventCollectable ec)
        {
            playerQuestDecisionEvent?.Raise((interactSystem.NetId, false));
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
            int goldAmount,
            System.Action<bool> onComplete
        )
        {
            inventorySystem.OnPickUp(ec, itemId, onComplete);
            goldSystem.OnPickUp(ec, goldAmount, onComplete);
        }

        public override void OnSetUserId(IEventCollectable ec, string tokenId)
        {
            if (isResolvingCharacterId || !string.IsNullOrEmpty(stateStorage.CharacterId))
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
                if (inventorySystem.OnPurchase(ec, productId, amount))
                    goldSystem.ReduceGold(ec, product.price * amount);
            }
        }

        public override void OnTeleportAccepted(IEventCollectable ec, string portalId)
        {
            teleportSystem.OnTeleport(portalId);
        }

        // --- Private Methods ---

        private async Task ResolveAndSetUserIdFromTokenAsync(string tokenId)
        {
            isResolvingCharacterId = true;
            var splitedToken = tokenId.Split('_');
            if (splitedToken[0] == config.TestJoinToken) // TODO: add TEST flag for servers
            {
                var botId = splitedToken.Length > 1 ? splitedToken[1] : "UnknownBot";
                stateStorage.SetCharacterId($"bot_{botId}");
                gameObject.name = $"BotPlayer_{botId}";
                isResolvingCharacterId = false;
                return;
            }

            try
            {
                var consumeResponse = await playerService.ConsumeWorldJoinTokenAsync(
                    tokenId,
                    serverAccessToken
                );
                if (consumeResponse != null)
                    stateStorage.SetCharacterId(consumeResponse.user_id);
            }
            finally
            {
                isResolvingCharacterId = false;
            }
        }

        public override void OnSendMessage(IEventCollectable ec, string message)
        {
            chatSystem.OnSendMessage(ec, message);
        }
    }
}
