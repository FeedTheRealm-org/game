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

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            if (resolver.TryResolve<NpcInteractedEvent>(out var ev) && ev != null)
                npcInteractedEvent = ev;

            if (resolver.TryResolve<NpcQuestCompletedEvent>(out var qev) && qev != null)
            {
                npcQuestCompletedEvent = qev;
                npcQuestCompletedEvent.OnRaised += OnQuestCompletedGlobal;
            }

            if (resolver.TryResolve<PlayerQuestDecisionEvent>(out var pqde) && pqde != null)
            {
                playerQuestDecisionEvent = pqde;
                playerQuestDecisionEvent.OnRaised += OnQuestDecisionGlobal;
            }
        }

        private NpcInteractedEvent npcInteractedEvent;
        private NpcQuestCompletedEvent npcQuestCompletedEvent;
        private PlayerQuestDecisionEvent playerQuestDecisionEvent;

        private string npcId;
        public string NpcId => npcId;

        private WorldMonitor worldMonitor;
        private uint ownNetId;
        private CharacterStateStorage stateStorage;

        private NpcDialogProgressionTracker tracker;
        private NpcDialogInactivityTimer inactivityTimer;

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

            tracker = new NpcDialogProgressionTracker(npcId, npcDialogRegistry, logger);
            inactivityTimer = new NpcDialogInactivityTimer(this, inactivityTimeout);
        }

        private void OnDestroy()
        {
            if (npcQuestCompletedEvent != null)
                npcQuestCompletedEvent.OnRaised -= OnQuestCompletedGlobal;
            if (playerQuestDecisionEvent != null)
                playerQuestDecisionEvent.OnRaised -= OnQuestDecisionGlobal;
        }

        public bool CanInteract(IInteractor interactor) => true;

        public string Interact(IInteractor interactor)
        {
            uint playerNetId = interactor.NetId;

            var state = tracker.GetOrCreate(playerNetId);
            state.Interactor = interactor;
            state.MessageIndex = 0;

            _activeSessions.Add(playerNetId);
            UpdateMovementBlockedState();

            var connId = GetPlayerConnectionId(playerNetId);
            if (!connId.HasValue)
            {
                logger?.Log($"[NpcInteractSystem] conn not found, Player:{playerNetId}.", this);
                return npcId;
            }

            if (tracker.GetCurrentMessageCount(state) == 0)
            {
                logger?.Log(
                    $"[NpcInteractSystem] No messages for NPC '{npcId}' at progression {state.ProgressionIndex}.",
                    this
                );
                _activeSessions.Remove(playerNetId);
                UpdateMovementBlockedState();
                interactor.FinishInteracting();
                return npcId;
            }

            /*logger?.Log(
                $"[NpcInteractSystem] Player:{playerNetId} interacting with NPC:{npcId} (progression={state.ProgressionIndex}).",
                this
            );*/

            npcInteractedEvent?.Raise((playerNetId, npcId));
            SendDialogMessage(playerNetId, connId.Value, state, DialogStateType.DialogTypeStarted);
            return npcId;
        }

        public void ContinueInteraction(IInteractor interactor)
        {
            uint playerNetId = interactor.NetId;

            if (!_activeSessions.Contains(playerNetId))
                return;

            if (!tracker.TryGet(playerNetId, out var state))
                return;

            if (state.Phase == PlayerPhase.WaitingForQuestDecision)
            {
                logger?.Log(
                    $"[NpcInteractSystem] ContinueInteraction ignored — quest pending for Player:{playerNetId}.",
                    this
                );
                return;
            }

            var connId = GetPlayerConnectionId(playerNetId);
            if (!connId.HasValue)
                return;

            int nextIndex = state.MessageIndex + 1;

            if (nextIndex >= tracker.GetCurrentMessageCount(state))
            {
                // Current dialog exhausted
                if (state.Phase == PlayerPhase.InOnQuestAcceptedDialog)
                {
                    interactor.FinishInteracting();
                    return;
                }

                // Transition to onQuestAccepted dialog if quest was accepted this session
                if (
                    !string.IsNullOrEmpty(state.OnAcceptedDialogId)
                    && tracker.GetOnQuestAcceptedMessageCount(state) > 0
                )
                {
                    state.Phase = PlayerPhase.InOnQuestAcceptedDialog;
                    state.MessageIndex = 0;
                }

                interactor.FinishInteracting();
                return;
            }

            state.MessageIndex = nextIndex;
            SendDialogMessage(playerNetId, connId.Value, state, DialogStateType.DialogTypeAdvanced);
        }

        public void StopInteraction(IInteractor interactor)
        {
            uint playerNetId = interactor.NetId;

            if (!_activeSessions.Remove(playerNetId))
                return;

            inactivityTimer.Stop(playerNetId);
            UpdateMovementBlockedState();

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

        public void OnQuestDecided(uint playerNetId) => tracker.OnQuestDecided(playerNetId);

        public void OnQuestAccepted(uint playerNetId) => tracker.OnQuestAccepted(playerNetId);

        private void OnQuestDecisionGlobal((uint playerNetId, bool isAccepted) data)
        {
            if (tracker == null)
                return;

            if (!_activeSessions.Contains(data.playerNetId))
                return;

            if (data.isAccepted)
                tracker.OnQuestAccepted(data.playerNetId);

            tracker.OnQuestDecided(data.playerNetId);
        }

        private void OnQuestCompletedGlobal((uint playerNetId, string questId, string npcId) data)
        {
            if (data.npcId != this.npcId)
                return;

            tracker?.OnQuestCompleted(data.playerNetId, data.questId);
        }

        private void SendDialogMessage(
            uint playerNetId,
            int connId,
            PlayerDialogState state,
            DialogStateType dialogState
        )
        {
            string activeDialogId = tracker.GetActiveDialogId(state);
            string questId = tracker.GetQuestIdForCurrentMessage(state);
            bool hasQuest = !string.IsNullOrEmpty(questId);

            if (hasQuest && state.Phase != PlayerPhase.InOnQuestAcceptedDialog)
            {
                state.Phase = PlayerPhase.WaitingForQuestDecision;
                inactivityTimer.Stop(playerNetId);
            }
            else
            {
                state.Phase =
                    state.Phase == PlayerPhase.InOnQuestAcceptedDialog
                        ? PlayerPhase.InOnQuestAcceptedDialog
                        : PlayerPhase.Normal;
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
                        DialogId = activeDialogId,
                    },
                    connId
                )
            );
        }

        private void UpdateMovementBlockedState()
        {
            if (stateStorage != null)
                stateStorage.IsMovementBlocked = _activeSessions.Count > 0;
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
