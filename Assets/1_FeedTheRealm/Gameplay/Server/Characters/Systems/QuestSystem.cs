using System.Collections.Generic;
using System.Linq;
using Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Environment.Quest;
using FTR.Gameplay.Server.Utils;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    /// <summary>
    /// Server-side quest system for a single player entity.
    /// </summary>
    public class QuestSystem : MonoBehaviour
    {
        private Logging.Logger logger;
        private ServerQuestRegistry serverQuestRegistry;

        private EnemySlayedEvent enemySlayedEvent;
        private NpcInteractedEvent npcInteractedEvent;

        private uint netId;
        private WorldMonitor worldMonitor;
        private uint ownNetId;

        private QuestRewardGranter rewardGranter;

        private readonly Dictionary<string, QuestProgressState> activeQuests =
            new Dictionary<string, QuestProgressState>();

        private bool subscribedToEnemySlayed = false;
        private bool subscribedToNpcInteracted = false;

        [Inject]
        public void Construct(
            Logging.Logger logger,
            ServerQuestRegistry serverQuestRegistry,
            IObjectResolver resolver
        )
        {
            this.logger = logger;
            this.serverQuestRegistry = serverQuestRegistry;

            if (resolver.TryResolve<EnemySlayedEvent>(out var ev1) && ev1 != null)
                enemySlayedEvent = ev1;
            if (resolver.TryResolve<NpcInteractedEvent>(out var ev2) && ev2 != null)
                npcInteractedEvent = ev2;

            resolver.TryResolve<QuestRewardGoldEvent>(out var goldEvent);
            resolver.TryResolve<QuestRewardItemEvent>(out var itemEvent);

            _pendingGoldEvent = goldEvent;
            _pendingItemEvent = itemEvent;
        }

        private QuestRewardGoldEvent _pendingGoldEvent;
        private QuestRewardItemEvent _pendingItemEvent;

        public void Initialize(uint netId, WorldMonitor worldMonitor, uint ownNetId)
        {
            this.netId = netId;
            this.worldMonitor = worldMonitor;
            this.ownNetId = ownNetId;

            rewardGranter = new QuestRewardGranter(
                netId,
                logger,
                _pendingGoldEvent,
                _pendingItemEvent
            );
        }

        private void OnDestroy()
        {
            UnsubscribeFromEnemySlayed();
            UnsubscribeFromNpcInteracted();
        }

        public void OnQuestAccepted(IEventCollectable ec, string questId)
        {
            if (string.IsNullOrEmpty(questId))
            {
                logger?.Log("[QuestSystem] OnQuestAccepted called with empty questId.", this);
                return;
            }

            if (!serverQuestRegistry.TryGetQuest(questId, out var questData))
            {
                logger?.Log(
                    $"[QuestSystem] Quest '{questId}' not found in registry.",
                    this,
                    Logging.LogType.Warning
                );
                return;
            }

            if (activeQuests.ContainsKey(questId))
            {
                return;
            }

            var state = new QuestProgressState(questData);
            activeQuests[questId] = state;

            if (questData.type == QuestType.EnemySlays)
                SubscribeToEnemySlayed();
            else if (questData.type == QuestType.NpcInteract)
                SubscribeToNpcInteracted();

            SendProgressEvent(state);
        }

        private void OnEnemySlayed((uint killerNetId, string enemyTypeId) data)
        {
            if (data.killerNetId != netId)
                return;

            if (
                ProcessQuestsProgress(QuestType.EnemySlays, data.enemyTypeId)
                && !HasActiveQuestsOfType(QuestType.EnemySlays)
            )
                UnsubscribeFromEnemySlayed();
        }

        private void OnNpcInteracted((uint playerNetId, string npcId) data)
        {
            if (data.playerNetId != netId)
                return;

            if (
                ProcessQuestsProgress(QuestType.NpcInteract, data.npcId)
                && !HasActiveQuestsOfType(QuestType.NpcInteract)
            )
                UnsubscribeFromNpcInteracted();
        }

        private bool ProcessQuestsProgress(QuestType type, string targetId)
        {
            bool anyUpdated = false;
            var completedQuests = new List<QuestProgressState>();

            foreach (var state in activeQuests.Values)
            {
                if (
                    state.Quest.type != type
                    || state.Quest.targetId != targetId
                    || state.IsCompleted
                )
                    continue;

                state.Increment();
                anyUpdated = true;

                SendProgressEvent(state);

                if (state.IsCompleted)
                    completedQuests.Add(state);
            }

            foreach (var state in completedQuests)
                CompleteQuest(state);

            return anyUpdated;
        }

        private void CompleteQuest(QuestProgressState state)
        {
            activeQuests.Remove(state.Quest.id);

            var connId = GetConnectionId();
            if (connId.HasValue)
            {
                worldMonitor.Events.Enqueue(
                    new QuestCompletedEvent(
                        ownNetId,
                        connId.Value,
                        new QuestCompletedEventContent { QuestId = state.Quest.id }
                    )
                );
            }

            rewardGranter.Grant(state.Quest);
        }

        private void SendProgressEvent(QuestProgressState state)
        {
            var connId = GetConnectionId();
            if (!connId.HasValue)
                return;

            worldMonitor.Events.Enqueue(
                new QuestProgressEvent(
                    ownNetId,
                    connId.Value,
                    new QuestProgressEventContent
                    {
                        QuestId = state.Quest.id,
                        Current = state.Current,
                        Target = state.Target,
                    }
                )
            );
        }

        private int? GetConnectionId()
        {
            if (worldMonitor.Entities.TryGet(netId, out var entity) && entity.ConnectionId.HasValue)
                return entity.ConnectionId.Value;

            logger?.Log(
                $"[QuestSystem] Connection not found for Player:{netId}.",
                this,
                Logging.LogType.Warning
            );
            return null;
        }

        private bool HasActiveQuestsOfType(QuestType type) =>
            activeQuests.Values.Any(s => s.Quest.type == type && !s.IsCompleted);

        private void SubscribeToEnemySlayed()
        {
            if (subscribedToEnemySlayed || enemySlayedEvent == null)
                return;
            enemySlayedEvent.OnRaised += OnEnemySlayed;
            subscribedToEnemySlayed = true;
        }

        private void UnsubscribeFromEnemySlayed()
        {
            if (!subscribedToEnemySlayed || enemySlayedEvent == null)
                return;
            enemySlayedEvent.OnRaised -= OnEnemySlayed;
            subscribedToEnemySlayed = false;
        }

        private void SubscribeToNpcInteracted()
        {
            if (subscribedToNpcInteracted || npcInteractedEvent == null)
                return;
            npcInteractedEvent.OnRaised += OnNpcInteracted;
            subscribedToNpcInteracted = true;
        }

        private void UnsubscribeFromNpcInteracted()
        {
            if (!subscribedToNpcInteracted || npcInteractedEvent == null)
                return;
            npcInteractedEvent.OnRaised -= OnNpcInteracted;
            subscribedToNpcInteracted = false;
        }
    }
}
