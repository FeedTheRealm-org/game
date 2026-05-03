using System;
using System.Collections.Generic;
using System.Linq;
using Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Core.Server.Persistence.Schemas;
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
        public event Action<string, int, bool> OnSaveQuestProgress;

        private Logging.Logger logger;
        private ServerQuestRegistry serverQuestRegistry;
        private QuestRewardItemEvent questRewardItemEvent;
        private QuestRewardGoldEvent questRewardGoldEvent;
        private EnemySlayedEvent enemySlayedEvent;
        private NpcInteractedEvent npcInteractedEvent;
        private NpcQuestCompletedEvent npcQuestCompletedEvent;
        private PlayerQuestDecisionEvent playerQuestDecisionEvent;

        private uint netId;
        private WorldMonitor worldMonitor;
        private uint ownNetId;

        private QuestRewardGranter rewardGranter;

        private readonly Dictionary<string, QuestProgressState> activeQuests =
            new Dictionary<string, QuestProgressState>();
        private readonly HashSet<string> completedQuests = new HashSet<string>();

        private bool subscribedToEnemySlayed = false;
        private bool subscribedToNpcInteracted = false;

        [Inject]
        public void Construct(
            Logging.Logger logger,
            ServerQuestRegistry serverQuestRegistry,
            QuestRewardGoldEvent questRewardGoldEvent,
            QuestRewardItemEvent questRewardItemEvent,
            EnemySlayedEvent enemySlayedEvent,
            NpcInteractedEvent npcInteractedEvent,
            NpcQuestCompletedEvent npcQuestCompletedEvent,
            IObjectResolver resolver
        )
        {
            this.logger = logger;
            this.serverQuestRegistry = resolver.Resolve<ServerQuestRegistry>();
            this.questRewardGoldEvent = resolver.Resolve<QuestRewardGoldEvent>();
            this.questRewardItemEvent = resolver.Resolve<QuestRewardItemEvent>();
            this.enemySlayedEvent = resolver.Resolve<EnemySlayedEvent>();
            this.npcInteractedEvent = resolver.Resolve<NpcInteractedEvent>();
            this.npcQuestCompletedEvent = resolver.Resolve<NpcQuestCompletedEvent>();
            this.playerQuestDecisionEvent = resolver.Resolve<PlayerQuestDecisionEvent>();
        }

        public void Initialize(uint netId, WorldMonitor worldMonitor, uint ownNetId)
        {
            this.netId = netId;
            this.worldMonitor = worldMonitor;
            this.ownNetId = ownNetId;

            rewardGranter = new QuestRewardGranter(
                netId,
                logger,
                questRewardGoldEvent,
                questRewardItemEvent
            );
        }

        private void OnDestroy()
        {
            UnsubscribeFromEnemySlayed();
            UnsubscribeFromNpcInteracted();
        }

        public void LoadQuests(List<QuestModel> activeQuests, List<string> completedQuests)
        {
            foreach (var quest in activeQuests)
            {
                var questId = quest.EffectiveQuestId.Split('_')[0];
                var npcId = quest.EffectiveQuestId.Split('_')[1];
                if (!serverQuestRegistry.TryGetQuest(questId, out var questData))
                    continue;
                var state = new QuestProgressState(questData, npcId, quest.Progress);
                this.activeQuests[state.EffectiveQuestId] = state;

                if (questData.type == QuestType.EnemySlays)
                    SubscribeToEnemySlayed();
                else if (questData.type == QuestType.NpcInteract)
                    SubscribeToNpcInteracted();

                SendProgressEvent(state);
            }
            foreach (var questId in completedQuests)
                this.completedQuests.Add(questId);
        }

        public void OnQuestAccepted(IEventCollectable ec, string questId, string npcId = "")
        {
            playerQuestDecisionEvent?.Raise((netId, true));

            if (string.IsNullOrEmpty(questId))
            {
                logger?.Log("[QuestSystem] OnQuestAccepted called with empty questId.", this);
                return;
            }

            string effectiveQuestId = string.IsNullOrEmpty(npcId) ? questId : $"{questId}_{npcId}";

            if (!serverQuestRegistry.TryGetQuest(questId, out var questData))
            {
                logger?.Log(
                    $"[QuestSystem] Quest '{questId}' not found in registry.",
                    this,
                    Logging.LogType.Warning
                );
                return;
            }

            if (activeQuests.ContainsKey(effectiveQuestId))
                return;

            if (completedQuests.Contains(effectiveQuestId))
            {
                completedQuests.Remove(effectiveQuestId);
            }

            var state = new QuestProgressState(questData, npcId);
            activeQuests[effectiveQuestId] = state;

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
            var connId = GetConnectionId();
            if (!connId.HasValue)
            {
                logger?.Log(
                    $"[QuestSystem] Cannot complete quest '{state.Quest.id}' — no connection for Player:{netId}.",
                    this,
                    Logging.LogType.Warning
                );
                return;
            }

            activeQuests.Remove(state.EffectiveQuestId);
            completedQuests.Add(state.EffectiveQuestId);

            worldMonitor.Events.Enqueue(
                new QuestCompletedEvent(
                    ownNetId,
                    connId.Value,
                    new QuestCompletedEventContent
                    {
                        QuestId = state.Quest.id,
                        EffectiveQuestId = state.EffectiveQuestId,
                    }
                )
            );

            rewardGranter.Grant(state.Quest);

            npcQuestCompletedEvent?.Raise((netId, state.Quest.id, state.NpcId));
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
                        EffectiveQuestId = state.EffectiveQuestId,
                    }
                )
            );
            OnSaveQuestProgress?.Invoke(state.EffectiveQuestId, state.Current, state.IsCompleted);
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

        public bool IsQuestCompleted(string questId, string npcId = "")
        {
            string effectiveId = string.IsNullOrEmpty(npcId) ? questId : $"{questId}_{npcId}";
            return completedQuests.Contains(effectiveId);
        }

        public bool IsQuestActive(string questId, string npcId = "")
        {
            string effectiveId = string.IsNullOrEmpty(npcId) ? questId : $"{questId}_{npcId}";
            return activeQuests.TryGetValue(effectiveId, out var state) && !state.IsCompleted;
        }

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
