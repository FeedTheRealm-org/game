using System.Collections;
using System.Collections.Generic;
using FTR.Core.Common.Interactions;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.Environment.Dialogs;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class NpcInteractSystem : MonoBehaviour, IInteractable
    {
        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private NpcDialogRegistry npcDialogRegistry;

        [SerializeField]
        private float inactivityTimeout = 10f;

        private string npcId;
        private WorldMonitor worldMonitor;
        private uint ownNetId;

        private Dictionary<uint, int> playerDialogStates = new Dictionary<uint, int>();
        private Dictionary<uint, Coroutine> playerTimeouts = new Dictionary<uint, Coroutine>();

        public void Initialize(
            Logging.Logger logger,
            NpcDialogRegistry npcDialogRegistry,
            WorldMonitor worldMonitor,
            uint ownNetId,
            string npcId
        )
        {
            this.logger = logger;
            this.npcDialogRegistry = npcDialogRegistry;
            this.worldMonitor = worldMonitor;
            this.ownNetId = ownNetId;
            this.npcId = npcId;
        }

        private int? GetPlayerConnectionId(uint playerNetId)
        {
            if (
                worldMonitor.Entities.TryGet(playerNetId, out var entity)
                && entity.ConnectionId.HasValue
            )
            {
                Debug.Log(
                    $"[NpcInteractSystem] Found player connection for netId:{playerNetId} connectionId:{entity.ConnectionId.Value}"
                );
                return entity.ConnectionId.Value;
            }
            Debug.LogWarning(
                $"[NpcInteractSystem] Player connection NOT found for netId:{playerNetId}"
            );
            return null;
        }

        public string Interact(IInteractor interactor)
        {
            uint playerNetId = interactor.NetId;

            int count = npcDialogRegistry.GetMessageCount(npcId);
            if (count == 0)
            {
                if (logger != null)
                    logger.Log(
                        $"[NpcInteractSystem] No messages registered for NpcId '{npcId}'.",
                        this
                    );
                return npcId;
            }

            playerDialogStates[playerNetId] = 0;
            RestartInactivityTimer(playerNetId, interactor);

            worldMonitor.Events.Enqueue(
                new DialogEvent(
                    ownNetId,
                    new DialogEventContent
                    {
                        DialogState = DialogStateType.DialogTypeStarted,
                        NpcId = npcId,
                        DialogIndex = 0,
                    },
                    GetPlayerConnectionId(playerNetId)
                )
            );

            if (logger != null)
                logger.Log($"NPC interacted with by {interactor.GameObject.name}", this);

            return npcId;
        }

        public void ContinueInteraction(IInteractor interactor)
        {
            uint playerNetId = interactor.NetId;

            if (!playerDialogStates.TryGetValue(playerNetId, out int currentIndex))
                return;

            int count = npcDialogRegistry.GetMessageCount(npcId);
            int nextIndex = currentIndex + 1;

            if (nextIndex >= count)
            {
                interactor.FinishInteracting();
                return;
            }

            playerDialogStates[playerNetId] = nextIndex;
            RestartInactivityTimer(playerNetId, interactor);

            worldMonitor.Events.Enqueue(
                new DialogEvent(
                    ownNetId,
                    new DialogEventContent
                    {
                        DialogState = DialogStateType.DialogTypeAdvanced,
                        NpcId = npcId,
                        DialogIndex = nextIndex,
                    },
                    GetPlayerConnectionId(playerNetId)
                )
            );
        }

        public void StopInteraction(IInteractor interactor)
        {
            uint playerNetId = interactor.NetId;

            if (!playerDialogStates.ContainsKey(playerNetId))
                return;

            playerDialogStates.Remove(playerNetId);
            StopInactivityTimer(playerNetId);

            worldMonitor.Events.Enqueue(
                new DialogEvent(
                    ownNetId,
                    new DialogEventContent
                    {
                        DialogState = DialogStateType.DialogTypeClosed,
                        NpcId = npcId,
                        DialogIndex = 0,
                    },
                    GetPlayerConnectionId(playerNetId)
                )
            );
        }

        public bool CanInteract(IInteractor interactor)
        {
            return true;
        }

        private void RestartInactivityTimer(uint playerNetId, IInteractor interactor)
        {
            StopInactivityTimer(playerNetId);
            playerTimeouts[playerNetId] = StartCoroutine(
                InactivityCoroutine(playerNetId, interactor)
            );
        }

        private void StopInactivityTimer(uint playerNetId)
        {
            if (
                playerTimeouts.TryGetValue(playerNetId, out Coroutine coroutine)
                && coroutine != null
            )
            {
                StopCoroutine(coroutine);
                playerTimeouts.Remove(playerNetId);
            }
        }

        private IEnumerator InactivityCoroutine(uint playerNetId, IInteractor interactor)
        {
            yield return new WaitForSeconds(inactivityTimeout);

            if (playerDialogStates.ContainsKey(playerNetId))
            {
                interactor.FinishInteracting();
            }
        }
    }
}
