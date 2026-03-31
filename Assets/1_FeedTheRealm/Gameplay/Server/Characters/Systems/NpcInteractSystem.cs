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

        // Current dialog index per player.
        private Dictionary<uint, int> playerDialogStates = new Dictionary<uint, int>();

        // Players currently waiting on a quest prompt decision.
        // DialogNext commands are ignored for these players until OnQuestDecided is called.
        private HashSet<uint> _questPendingPlayers = new HashSet<uint>();

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
                return entity.ConnectionId.Value;

            return null;
        }

        // -------------------------------------------------------------------------
        // IInteractable
        // -------------------------------------------------------------------------

        public string Interact(IInteractor interactor)
        {
            uint playerNetId = interactor.NetId;

            int count = npcDialogRegistry.GetMessageCount(npcId);
            if (count == 0)
            {
                logger?.Log($"[NpcInteractSystem] No messages for Npc '{npcId}'.", this);
                return npcId;
            }

            playerDialogStates[playerNetId] = 0;
            _questPendingPlayers.Remove(playerNetId);

            var connId = GetPlayerConnectionId(playerNetId);
            if (!connId.HasValue)
            {
                logger?.Log($"[NpcInteractSystem] conn not found, Player:{playerNetId}.", this);
                return npcId;
            }

            var questId = npcDialogRegistry.GetQuestIdAt(npcId, 0);

            if (!string.IsNullOrEmpty(questId))
                _questPendingPlayers.Add(playerNetId);
            else
                RestartInactivityTimer(playerNetId, interactor);

            worldMonitor.Events.Enqueue(
                new DialogEvent(
                    ownNetId,
                    new DialogEventContent
                    {
                        DialogState = DialogStateType.DialogTypeStarted,
                        NpcId = npcId,
                        DialogIndex = 0,
                        QuestId = questId,
                    },
                    connId.Value
                )
            );

            logger?.Log(
                $"[NpcInteractSystem] NPC interacted with by {interactor.GameObject.name}",
                this
            );
            return npcId;
        }

        public void ContinueInteraction(IInteractor interactor)
        {
            uint playerNetId = interactor.NetId;

            if (_questPendingPlayers.Contains(playerNetId))
            {
                logger?.Log(
                    $"[NpcInteractSystem] DialogNext ignored — quest pending for Player:{playerNetId}.",
                    this
                );
                return;
            }

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

            var questId = npcDialogRegistry.GetQuestIdAt(npcId, nextIndex);

            if (!string.IsNullOrEmpty(questId))
                _questPendingPlayers.Add(playerNetId);
            else
                RestartInactivityTimer(playerNetId, interactor);

            worldMonitor.Events.Enqueue(
                new DialogEvent(
                    ownNetId,
                    new DialogEventContent
                    {
                        DialogState = DialogStateType.DialogTypeAdvanced,
                        NpcId = npcId,
                        DialogIndex = nextIndex,
                        QuestId = questId,
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
            _questPendingPlayers.Remove(playerNetId);
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

        /// <summary>
        /// Clears the quest-pending block for this player so the DialogNext that
        /// QuestView dispatches immediately after the decision can advance the dialog.
        /// Call this from the server transaction handler when AcceptQuest or RejectQuest
        /// arrives, BEFORE the paired DialogNext is processed.
        /// </summary>
        public void OnQuestDecided(uint playerNetId)
        {
            if (!_questPendingPlayers.Remove(playerNetId))
                return;

            logger?.Log(
                $"[NpcInteractSystem] Quest decided — unblocking dialog for Player:{playerNetId}.",
                this
            );
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
                interactor.FinishInteracting();
        }
    }
}
