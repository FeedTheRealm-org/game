using System.Collections;
using System.Collections.Generic;
using FTR.Core.Common.Interactions;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Common.Environment.Npcs;
using FTR.Gameplay.Server.Characters.Interactions;
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

        private Dictionary<uint, int> playerDialogStates = new Dictionary<uint, int>();
        private Dictionary<uint, Coroutine> playerTimeouts = new Dictionary<uint, Coroutine>();

        private WorldMonitor worldMonitor;

        public void Initialize(
            Logging.Logger logger,
            NpcDialogRegistry npcDialogRegistry,
            WorldMonitor worldMonitor
        )
        {
            this.logger = logger;
            this.npcDialogRegistry = npcDialogRegistry;
            this.worldMonitor = worldMonitor;
        }

        private void Awake()
        {
            npcIdentity = GetComponent<NpcIdentity>();
        }

        public string Interact(IInteractor interactor)
        {
            if (interactor is IServerInteractor serverInteractor)
            {
                var ec = serverInteractor.CurrentEventCollector;
                if (ec == null)
                    return npcIdentity.NpcId;

                uint netId = serverInteractor.NetId;

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

                // Start or restart dialog
                playerDialogStates[netId] = 0;
                RestartInactivityTimer(netId, interactor);

                ec.Collect(
                    new DialogEvent(
                        netId,
                        new DialogEventContent
                        {
                            DialogState = DialogStateType.DialogTypeStarted,
                            NpcId = npcIdentity.NpcId,
                            DialogIndex = 0,
                        }
                    )
                );

                if (logger != null)
                    logger.Log($"NPC interacted with by " + interactor.GameObject.name, this);
            }

            return npcIdentity?.NpcId ?? "";
        }

        public void ContinueInteraction(IInteractor interactor)
        {
            if (interactor is IServerInteractor serverInteractor)
            {
                var ec = serverInteractor.CurrentEventCollector;
                if (ec == null)
                    return;

                uint netId = serverInteractor.NetId;

                if (!playerDialogStates.TryGetValue(netId, out int currentIndex))
                {
                    return;
                }

                int count = npcDialogRegistry.GetMessageCount(npcIdentity.NpcId);
                int nextIndex = currentIndex + 1;

                if (nextIndex >= count)
                {
                    // Interaction ends if no more dialog content
                    interactor.FinishInteracting();
                    return;
                }

                playerDialogStates[netId] = nextIndex;
                RestartInactivityTimer(netId, interactor);

                ec.Collect(
                    new DialogEvent(
                        netId,
                        new DialogEventContent
                        {
                            DialogState = DialogStateType.DialogTypeAdvanced,
                            NpcId = npcIdentity.NpcId,
                            DialogIndex = nextIndex,
                        }
                    )
                );
            }
        }

        public void StopInteraction(IInteractor interactor)
        {
            if (interactor is IServerInteractor serverInteractor)
            {
                uint netId = serverInteractor.NetId;

                if (playerDialogStates.ContainsKey(netId))
                {
                    playerDialogStates.Remove(netId);
                    StopInactivityTimer(netId);

                    var closeEvent = new DialogEvent(
                        netId,
                        new DialogEventContent
                        {
                            DialogState = DialogStateType.DialogTypeClosed,
                            NpcId = npcIdentity.NpcId,
                            DialogIndex = 0,
                        }
                    );

                    if (worldMonitor != null)
                    {
                        worldMonitor.Events.Enqueue(closeEvent);
                    }
                    else
                    {
                        var ec = serverInteractor.CurrentEventCollector;
                        if (ec != null)
                        {
                            ec.Collect(closeEvent);
                        }
                    }
                }
            }
        }

        public bool CanInteract(IInteractor interactor)
        {
            return true;
        }

        private void RestartInactivityTimer(uint netId, IInteractor interactor)
        {
            StopInactivityTimer(netId);
            Coroutine coroutine = StartCoroutine(InactivityCoroutine(netId, interactor));
            playerTimeouts[netId] = coroutine;
        }

        private void StopInactivityTimer(uint netId)
        {
            if (playerTimeouts.TryGetValue(netId, out Coroutine coroutine) && coroutine != null)
            {
                StopCoroutine(coroutine);
                playerTimeouts.Remove(netId);
            }
        }

        private IEnumerator InactivityCoroutine(uint netId, IInteractor interactor)
        {
            yield return new WaitForSeconds(inactivityTimeout);

            if (playerDialogStates.ContainsKey(netId))
            {
                interactor.FinishInteracting();
            }
        }
    }
}
