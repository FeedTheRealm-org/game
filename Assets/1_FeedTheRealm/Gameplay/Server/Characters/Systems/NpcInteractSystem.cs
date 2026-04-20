using System.Collections.Generic;
using FTR.Core.Common.Interactions;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Enums;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Server.Utils;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    /// <summary>
    /// Server-side interactable for passive NPCs with dialog progression.
    /// </summary>
    public class NpcInteractSystem : MonoBehaviour, IInteractable, IQuestBlockable
    {
        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private NpcDialogRegistry npcDialogRegistry;

        [SerializeField]
        private float inactivityTimeout = 10f;

        private NpcInteractedEvent npcInteractedEvent;
        private PlayerQuestDecisionEvent playerQuestDecisionEvent;

        [Inject]
        public void Construct(
            IObjectResolver resolver,
            NpcInteractedEvent npcInteractedEvent,
            PlayerQuestDecisionEvent playerQuestDecisionEvent
        )
        {
            this.npcInteractedEvent = resolver.Resolve<NpcInteractedEvent>();
            this.playerQuestDecisionEvent = resolver.Resolve<PlayerQuestDecisionEvent>();
        }

        private string npcId;
        public string NpcId => npcId;

        private WorldMonitor worldMonitor;
        private uint ownNetId;
        private CharacterStateStorage stateStorage;

        private NpcDialogInactivityTimer inactivityTimer;

        private readonly Dictionary<uint, PlayerDialogState> _sessions =
            new Dictionary<uint, PlayerDialogState>();

        private readonly HashSet<uint> _activeSessions = new HashSet<uint>();

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
            this.stateStorage = GetComponent<CharacterStateStorage>();

            inactivityTimer = new NpcDialogInactivityTimer(this, inactivityTimeout);
            playerQuestDecisionEvent.OnRaised += OnQuestDecisionGlobal;
        }

        private void OnDestroy()
        {
            if (playerQuestDecisionEvent != null)
                playerQuestDecisionEvent.OnRaised -= OnQuestDecisionGlobal;

            inactivityTimer?.StopAll();
        }

        public bool CanInteract(IInteractor interactor) => true;

        public string Interact(IInteractor interactor)
        {
            uint playerNetId = interactor.NetId;

            if (_activeSessions.Contains(playerNetId))
            {
                ContinueSession(playerNetId, interactor);
                return npcId;
            }

            var questSystem = interactor.GameObject.GetComponent<QuestSystem>();
            string activeDialogId = ResolveActiveDialog(questSystem);

            if (string.IsNullOrEmpty(activeDialogId))
            {
                logger?.Log(
                    $"[NpcInteractSystem] No dialog resolved for NPC '{npcId}', Player:{playerNetId}.",
                    this
                );
                interactor.FinishInteracting();
                return npcId;
            }

            if (
                !npcDialogRegistry.TryGetMessagesByDialogId(activeDialogId, out var messages)
                || messages.Count == 0
            )
            {
                logger?.Log(
                    $"[NpcInteractSystem] Dialog '{activeDialogId}' has no messages for NPC '{npcId}'.",
                    this
                );
                interactor.FinishInteracting();
                return npcId;
            }

            if (!_sessions.TryGetValue(playerNetId, out var state))
            {
                state = new PlayerDialogState();
                _sessions[playerNetId] = state;
            }

            state.Interactor = interactor;
            state.MessageIndex = 0;
            state.Phase = PlayerPhase.Normal;
            state.ActiveDialogId = activeDialogId;

            _activeSessions.Add(playerNetId);

            var connId = GetPlayerConnectionId(playerNetId);
            if (!connId.HasValue)
            {
                logger?.Log($"[NpcInteractSystem] conn not found, Player:{playerNetId}.", this);
                return npcId;
            }

            npcInteractedEvent?.Raise((playerNetId, npcId));
            SendDialogMessage(playerNetId, connId.Value, state, DialogStateType.DialogTypeStarted);
            return npcId;
        }

        public void StopInteraction(IInteractor interactor)
        {
            uint playerNetId = interactor.NetId;

            _sessions.Remove(playerNetId);
            if (!_activeSessions.Remove(playerNetId))
                return;

            inactivityTimer.Stop(playerNetId);

            var connId = GetPlayerConnectionId(playerNetId);
            if (connId.HasValue)
            {
                worldMonitor.Events.Enqueue(
                    new DialogEvent(
                        ownNetId,
                        new DialogEventContent
                        {
                            DialogState = DialogStateType.DialogTypeClosed,
                            NpcId = npcId,
                            DialogIndex = 0,
                        },
                        connId.Value
                    )
                );
            }
        }

        public void OnQuestDecided(uint playerNetId)
        {
            if (!_sessions.TryGetValue(playerNetId, out var state))
                return;
            if (state.Phase != PlayerPhase.WaitingForQuestDecision)
                return;
            state.Phase = PlayerPhase.Normal;
        }

        private void ContinueSession(uint playerNetId, IInteractor interactor)
        {
            if (!_sessions.TryGetValue(playerNetId, out var state))
                return;

            if (state.Phase == PlayerPhase.WaitingForQuestDecision)
            {
                logger?.Log(
                    $"[NpcInteractSystem] Continue ignored — quest pending for Player:{playerNetId}.",
                    this
                );
                return;
            }

            var connId = GetPlayerConnectionId(playerNetId);
            if (!connId.HasValue)
                return;

            int currentCount = GetCurrentMessageCount(state);
            int nextIndex = state.MessageIndex + 1;

            if (nextIndex >= currentCount)
            {
                interactor.FinishInteracting();
                return;
            }

            state.MessageIndex = nextIndex;
            SendDialogMessage(playerNetId, connId.Value, state, DialogStateType.DialogTypeAdvanced);
        }

        private string ResolveActiveDialog(QuestSystem questSystem)
        {
            int count = npcDialogRegistry.GetProgressionCount(npcId);

            if (count == 0)
                return string.Empty;

            for (int i = 0; i < count; i++)
            {
                string dialogId = npcDialogRegistry.GetDialogId(npcId, i);
                string questId = npcDialogRegistry.GetQuestIdForSlot(npcId, i);
                bool hasQuest = !string.IsNullOrEmpty(questId);

                if (!hasQuest)
                {
                    return dialogId;
                }

                bool completed =
                    questSystem != null && questSystem.IsQuestCompleted(questId, npcId);

                bool repeatable = npcDialogRegistry.IsRepeatableAt(npcId, i);

                if (completed && !repeatable)
                    continue;

                bool active = questSystem != null && questSystem.IsQuestActive(questId, npcId);

                if (active)
                {
                    string onAccepted = npcDialogRegistry.GetOnQuestAcceptedDialogId(npcId, i);
                    if (!string.IsNullOrEmpty(onAccepted))
                    {
                        return onAccepted;
                    }
                }

                return dialogId;
            }

            return string.Empty;
        }

        private void OnQuestDecisionGlobal((uint playerNetId, bool isAccepted) data)
        {
            if (playerQuestDecisionEvent == null)
                return;
            if (!_activeSessions.Contains(data.playerNetId))
                return;

            OnQuestDecided(data.playerNetId);
        }

        private void SendDialogMessage(
            uint playerNetId,
            int connId,
            PlayerDialogState state,
            DialogStateType dialogState
        )
        {
            string questId = GetQuestIdForCurrentMessage(state);
            bool hasQuest = !string.IsNullOrEmpty(questId);

            if (hasQuest)
            {
                state.Phase = PlayerPhase.WaitingForQuestDecision;
                inactivityTimer.Stop(playerNetId);
            }
            else
            {
                state.Phase = PlayerPhase.Normal;
                inactivityTimer.Restart(playerNetId, state.Interactor);
            }

            worldMonitor.Events.Enqueue(
                new DialogEvent(
                    ownNetId,
                    new DialogEventContent
                    {
                        DialogState = dialogState,
                        NpcId = npcId,
                        DialogIndex = state.MessageIndex,
                        QuestId = hasQuest ? questId : string.Empty,
                        DialogId = state.ActiveDialogId,
                    },
                    connId
                )
            );
        }

        private string GetQuestIdForCurrentMessage(PlayerDialogState state)
        {
            return npcDialogRegistry.GetQuestIdForDialogAndMessage(
                npcId,
                state.ActiveDialogId,
                state.MessageIndex
            );
        }

        private int GetCurrentMessageCount(PlayerDialogState state)
        {
            if (npcDialogRegistry.TryGetMessagesByDialogId(state.ActiveDialogId, out var msgs))
                return msgs.Count;
            return 0;
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
    }
}
