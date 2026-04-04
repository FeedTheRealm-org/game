using System;
using System.Text;
using System.Threading.Tasks;
using FTR.Core.Common.Config;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using UnityEngine;
using UnityEngine.Networking;

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
        private Config config;
        private bool isResolvingCharacterId;

        public void Initialize(
            MovementSystem movementSystem,
            DashSystem dashSystem,
            UseSystem useSystem,
            PlayerInteractSystem interactSystem,
            InventorySystem inventorySystem,
            QuestSystem questSystem,
            CharacterStateStorage stateStorage,
            Config config
        )
        {
            this.movementSystem = movementSystem;
            this.dashSystem = dashSystem;
            this.useSystem = useSystem;
            this.interactSystem = interactSystem;
            this.inventorySystem = inventorySystem;
            this.questSystem = questSystem;
            this.stateStorage = stateStorage;
            this.config = config;
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

            if (stateStorage == null)
            {
                Debug.LogError(
                    "[ServerPlayerCommandHandler] CharacterStateStorage is missing; cannot set character ID.",
                    this
                );
                return;
            }

            if (!string.IsNullOrEmpty(stateStorage.CharacterId))
            {
                // Character ID already resolved for this entity.
                return;
            }

            _ = ResolveAndSetUserIdFromTokenAsync(tokenId);
        }

        private async Task ResolveAndSetUserIdFromTokenAsync(string tokenId)
        {
            isResolvingCharacterId = true;
            try
            {
                var resolvedUserId = await ConsumeJoinTokenAsync(tokenId);
                if (string.IsNullOrWhiteSpace(resolvedUserId))
                    return;

                if (!Guid.TryParse(resolvedUserId, out _))
                {
                    Debug.LogWarning(
                        $"[ServerPlayerCommandHandler] Invalid user_id returned from consume endpoint: '{resolvedUserId}'.",
                        this
                    );
                    return;
                }

                stateStorage.SetCharacterId(resolvedUserId);
            }
            finally
            {
                isResolvingCharacterId = false;
            }
        }

        private async Task<string> ConsumeJoinTokenAsync(string tokenId)
        {
            if (config?.ApiConfig == null)
            {
                Debug.LogError(
                    "[ServerPlayerCommandHandler] Config.ApiConfig is missing; cannot resolve world join token.",
                    this
                );
                return null;
            }

            var url =
                $"http://{config.ApiConfig.Hostname}:{config.ApiConfig.Port}/player/world-access/token/consume";
            var payload = JsonUtility.ToJson(
                new ConsumeWorldJoinTokenRequest { token_id = tokenId }
            );

            var uwr = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrWhiteSpace(config.ServerAccessToken))
            {
                uwr.SetRequestHeader("Authorization", $"Bearer {config.ServerAccessToken}");
            }

            await uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;
            if (
                uwr.result == UnityWebRequest.Result.ConnectionError
                || uwr.result == UnityWebRequest.Result.ProtocolError
            )
            {
                var error = string.IsNullOrEmpty(responseText)
                    ? null
                    : JsonUtility.FromJson<BackendErrorResponse>(responseText);
                Debug.LogWarning(
                    $"[ServerPlayerCommandHandler] Failed to consume world join token: {(error != null ? error.detail : responseText)}",
                    this
                );
                return null;
            }

            var envelope = JsonUtility.FromJson<ConsumeWorldJoinTokenEnvelope>(responseText);
            return envelope?.data?.user_id;
        }

        [Serializable]
        private class ConsumeWorldJoinTokenRequest
        {
            public string token_id;
        }

        [Serializable]
        private class ConsumeWorldJoinTokenResponse
        {
            public string user_id;
        }

        [Serializable]
        private class ConsumeWorldJoinTokenEnvelope
        {
            public ConsumeWorldJoinTokenResponse data;
        }

        [Serializable]
        private class BackendErrorResponse
        {
            public string detail;
        }
    }
}
