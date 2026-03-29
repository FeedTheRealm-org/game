using System.Collections;
using System.Collections.Generic;
using FTR.Core.Common.Interactions;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Common.Environment.Npcs;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems
{
    [RequireComponent(typeof(NpcIdentity))]
    public class NpcInteractSystem : MonoBehaviour, IInteractable
    {
        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private NpcDialogRegistry npcDialogRegistry;

        [SerializeField]
        private float inactivityTimeout = 10f;

        private NpcIdentity npcIdentity;
        private WorldMonitor worldMonitor;
        private uint ownNetId;

        private Dictionary<uint, int> playerDialogStates = new Dictionary<uint, int>();
        private Dictionary<uint, Coroutine> playerTimeouts = new Dictionary<uint, Coroutine>();

        public void Initialize(
            Logging.Logger logger,
            NpcDialogRegistry npcDialogRegistry,
            WorldMonitor worldMonitor,
            uint ownNetId
        )
        {
            this.logger = logger;
            this.npcDialogRegistry = npcDialogRegistry;
            this.worldMonitor = worldMonitor;
            this.ownNetId = ownNetId;
        }

        private void Awake()
        {
            npcIdentity = GetComponent<NpcIdentity>();
        }

        /// <summary>
        /// Resolves the connection ID of the player interactor from the EntityRegistry.
        /// The DialogEvent is sent via the NPC's NetworkAdapter, targeted to the player's connection,
        /// so only the interacting client receives it.
        /// </summary>
        private int? GetPlayerConnectionId(uint playerNetId)
        {
            if (worldMonitor.Entities.TryGet(playerNetId, out var entity))
            {
                Debug.Log(
                    $"[NpcInteractSystem] Found player entity for netId:{playerNetId} connectionId:{entity.NetworkAdapter.ConnectionId}"
                );
                return entity.NetworkAdapter.ConnectionId;
            }
            Debug.LogWarning(
                $"[NpcInteractSystem] Player entity NOT found for netId:{playerNetId}"
            );
            return null;
        }

        public string Interact(IInteractor interactor)
        {
            uint playerNetId = interactor.NetId;

            int count = npcDialogRegistry.GetMessageCount(npcIdentity.NpcId);
            if (count == 0)
            {
                if (logger != null)
                    logger.Log(
                        $"[NpcInteractSystem] No messages registered for NpcId '{npcIdentity.NpcId}'.",
                        this
                    );
                return npcIdentity.NpcId;
            }

            playerDialogStates[playerNetId] = 0;
            RestartInactivityTimer(playerNetId, interactor);

            worldMonitor.Events.Enqueue(
                new DialogEvent(
                    ownNetId,
                    new DialogEventContent
                    {
                        DialogState = DialogStateType.DialogTypeStarted,
                        NpcId = npcIdentity.NpcId,
                        DialogIndex = 0,
                    },
                    GetPlayerConnectionId(playerNetId)
                )
            );

            if (logger != null)
                logger.Log($"NPC interacted with by {interactor.GameObject.name}", this);

            return npcIdentity.NpcId;
        }

        public void ContinueInteraction(IInteractor interactor)
        {
            uint playerNetId = interactor.NetId;

            if (!playerDialogStates.TryGetValue(playerNetId, out int currentIndex))
                return;

            int count = npcDialogRegistry.GetMessageCount(npcIdentity.NpcId);
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
                        NpcId = npcIdentity.NpcId,
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
                        NpcId = npcIdentity.NpcId,
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
