using System.Collections;
using System.Collections.Generic;
using FTR.Core.Common.Interactions;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    /// <summary>
    /// Server-side interactable for passive NPCs with dialog sequences.
    /// Implements IInteractable for the standard interaction lifecycle,
    /// IQuestBlockable to pause dialog while waiting for a quest decision,
    /// and publishes to NpcInteractedServerChannel so QuestSystem can track NpcInteract quests.
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
            var hasEvent = resolver.TryResolve<NpcInteractedEvent>(out var ev);
            if (hasEvent && ev != null)
                this.npcInteractedEvent = ev;
        }

        private NpcInteractedEvent npcInteractedEvent;

        private string npcId;
        private WorldMonitor worldMonitor;
        private uint ownNetId;
        private Dictionary<uint, int> playerDialogStates = new Dictionary<uint, int>();
        private HashSet<uint> _questPendingPlayers = new HashSet<uint>();
        private Dictionary<uint, Coroutine> playerTimeouts = new Dictionary<uint, Coroutine>();
        private CharacterStateStorage stateStorage;

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
        }

        private void UpdateMovementBlockedState()
        {
            if (stateStorage != null)
            {
                stateStorage.IsMovementBlocked = playerDialogStates.Count > 0;
            }
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
            UpdateMovementBlockedState();

            var connId = GetPlayerConnectionId(playerNetId);
            if (!connId.HasValue)
            {
                logger?.Log($"[NpcInteractSystem] conn not found, Player:{playerNetId}.", this);
                return npcId;
            }

            if (npcInteractedEvent == null)
            {
                logger?.Log(
                    "[NpcInteractSystem] npcInteractedEvent is NULL! Cannot notify QuestSystem.",
                    this,
                    Logging.LogType.Error
                );
            }
            else
            {
                logger?.Log(
                    $"[NpcInteractSystem] Raising NpcInteractedEvent for player {playerNetId} and npc {npcId}",
                    this
                );
                npcInteractedEvent?.Raise((playerNetId, npcId));
            }

            var questId = npcDialogRegistry.GetQuestIdAt(npcId, 0);

            if (!string.IsNullOrEmpty(questId))
            {
                _questPendingPlayers.Add(playerNetId);
                StopInactivityTimer(playerNetId);
            }
            else
            {
                RestartInactivityTimer(playerNetId, interactor);
            }

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

            logger?.Log($"[NpcInteractSystem] Interacted by {interactor.GameObject.name}", this);
            return npcId;
        }

        public void ContinueInteraction(IInteractor interactor)
        {
            uint playerNetId = interactor.NetId;

            if (_questPendingPlayers.Contains(playerNetId))
            {
                logger?.Log(
                    $"[NpcInteractSystem] ContinueInteraction ignored — quest pending for Player:{playerNetId}.",
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
            {
                _questPendingPlayers.Add(playerNetId);
                StopInactivityTimer(playerNetId);
            }
            else
            {
                RestartInactivityTimer(playerNetId, interactor);
            }

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
            UpdateMovementBlockedState();

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

        public bool CanInteract(IInteractor interactor) => true;

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
