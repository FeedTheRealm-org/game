using System;
using System.Threading.Tasks;
using API;
using FTR.Core.Common.Config;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Config;
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
        private ServerSecretsConfig secretsConfig;
        private bool isResolvingCharacterId;
        private GoldSystem goldSystem;
        private TeleportSystem teleportSystem;
        private ChatSystem chatSystem;
        private NetworkAdapter networkAdapter;

        private Config config;
        private ServerConfig serverConfig;
        private PlayerQuestDecisionEvent playerQuestDecisionEvent;

        [Inject]
        public void Construct(
            IObjectResolver resolver,
            Config config,
            ServerConfig serverConfig,
            ServerSecretsConfig secretsConfig
        )
        {
            if (resolver.TryResolve<PlayerQuestDecisionEvent>(out var ev) && ev != null)
            {
                playerQuestDecisionEvent = ev;
            }
            this.config = config;
            this.serverConfig = serverConfig;
            this.secretsConfig = secretsConfig;
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
            GoldSystem goldSystem,
            TeleportSystem teleportSystem,
            ChatSystem chatSystem,
            NetworkAdapter networkAdapter
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
            this.goldSystem = goldSystem;
            this.teleportSystem = teleportSystem;
            this.chatSystem = chatSystem;
            this.networkAdapter = networkAdapter;
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
                this.networkAdapter.DisconnectClient();
                return;
            }
            _ = ResolveAndSetUserIdFromTokenAsync(tokenId);
        }

        public override void OnPurchase(
            IEventCollectable ec,
            uint netId,
            string productId,
            int amount,
            string shopId
        )
        {
            if (amount <= 0)
            {
                Debug.LogWarning(
                    $"[ServerPlayerCommandHandler] Invalid purchase amount {amount} for product {productId} in shop {shopId}"
                );
                return;
            }

            var product = ServerShopRegistry.GetProductFromShop(shopId, productId);
            if (product == null)
            {
                Debug.LogWarning(
                    $"[ServerPlayerCommandHandler] Product not found: {productId} in shop {shopId}"
                );
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
            var splitedToken = tokenId.Split('_'); // test-join-token_uuid
            if (serverConfig.IsTestWorld && splitedToken[0] == config.BotJoinToken)
            {
                if (splitedToken.Length == 2)
                {
                    stateStorage.SetCharacterId(splitedToken[1]);
                    gameObject.name = $"BotPlayer_{splitedToken[1]}";
                }
                else
                {
                    this.networkAdapter.DisconnectClient();
                }

                isResolvingCharacterId = false;
                return;
            }

            if (!Guid.TryParse(tokenId, out Guid _))
            {
                this.networkAdapter.DisconnectClient();
                isResolvingCharacterId = false;
                return;
            }

            try
            {
                var consumeResponse = await playerService.ConsumeWorldJoinTokenAsync(
                    tokenId,
                    secretsConfig.ServerFixedToken
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
