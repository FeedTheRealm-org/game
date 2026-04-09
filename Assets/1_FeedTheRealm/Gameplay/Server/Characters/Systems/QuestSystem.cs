using System.Collections.Generic;
using System.Linq;
using Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Environment.Quest;
using FTR.Gameplay.Server.Registry;
using FTRShared.Runtime.Models;
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
            if (resolver.TryResolve<QuestRewardGoldEvent>(out var ev3) && ev3 != null)
                questRewardGoldEvent = ev3;
            if (resolver.TryResolve<QuestRewardItemEvent>(out var ev4) && ev4 != null)
                questRewardItemEvent = ev4;
        }

        private EnemySlayedEvent enemySlayedEvent;
        private NpcInteractedEvent npcInteractedEvent;
        private QuestRewardGoldEvent questRewardGoldEvent;
        private QuestRewardItemEvent questRewardItemEvent;

        private uint netId;
        private WorldMonitor worldMonitor;
        private uint ownNetId;

        private readonly Dictionary<string, QuestProgressState> activeQuests =
            new Dictionary<string, QuestProgressState>();

        private bool subscribedToEnemySlayed = false;
        private bool subscribedToNpcInteracted = false;

        public void Initialize(uint netId, WorldMonitor worldMonitor, uint ownNetId)
        {
            this.netId = netId;
            this.worldMonitor = worldMonitor;
            this.ownNetId = ownNetId;
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
                logger?.Log(
                    $"[QuestSystem] Quest '{questId}' already active for Player:{netId}.",
                    this
                );
                return;
            }

            var state = new QuestProgressState(questData);
            activeQuests[questId] = state;

            logger?.Log(
                $"[QuestSystem] Player:{netId} accepted '{questData.title}' "
                    + $"(type={questData.type}, rewards={questData.rewards?.Count ?? 0}).",
                this
            );

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

            bool anyUpdated = false;
            var completedQuests = new List<QuestProgressState>();

            foreach (var state in activeQuests.Values)
            {
                if (state.Quest.type != QuestType.EnemySlays)
                    continue;
                if (state.Quest.targetId != data.enemyTypeId)
                    continue;
                if (state.IsCompleted)
                    continue;

                state.Increment();
                anyUpdated = true;

                logger?.Log(
                    $"[QuestSystem] Player:{netId} '{state.Quest.title}': "
                        + $"{state.Current}/{state.Target}",
                    this
                );

                SendProgressEvent(state);

                if (state.IsCompleted)
                    completedQuests.Add(state);
            }

            foreach (var state in completedQuests)
                CompleteQuest(state);

            if (anyUpdated && !HasActiveQuestsOfType(QuestType.EnemySlays))
                UnsubscribeFromEnemySlayed();
        }

        private void OnNpcInteracted((uint playerNetId, string npcId) data)
        {
            if (data.playerNetId != netId)
                return;

            bool anyUpdated = false;
            var completedQuests = new List<QuestProgressState>();

            foreach (var state in activeQuests.Values)
            {
                if (state.Quest.type != QuestType.NpcInteract)
                    continue;
                if (state.Quest.targetId != data.npcId)
                    continue;
                if (state.IsCompleted)
                    continue;

                state.Increment();
                anyUpdated = true;

                logger?.Log(
                    $"[QuestSystem] Player:{netId} interacted with '{data.npcId}' "
                        + $"for quest '{state.Quest.title}'.",
                    this
                );

                SendProgressEvent(state);

                if (state.IsCompleted)
                    completedQuests.Add(state);
            }

            foreach (var state in completedQuests)
                CompleteQuest(state);

            if (anyUpdated && !HasActiveQuestsOfType(QuestType.NpcInteract))
                UnsubscribeFromNpcInteracted();
        }

        private void CompleteQuest(QuestProgressState state)
        {
            activeQuests.Remove(state.Quest.id);

            logger?.Log(
                $"[QuestSystem] Quest '{state.Quest.title}' completed for Player:{netId}.",
                this
            );

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

            GrantRewards(state.Quest);
        }

        private void GrantRewards(QuestData quest)
        {
            if (quest.rewards == null || quest.rewards.Count == 0)
                return;

            foreach (var reward in quest.rewards)
            {
                switch (reward.rewardType)
                {
                    case QuestRewardType.Gold:
                        if (reward.goldAmount > 0)
                        {
                            logger?.Log(
                                $"[QuestSystem] Granting {reward.goldAmount} gold to Player:{netId}.",
                                this
                            );
                            questRewardGoldEvent?.Raise((netId, reward.goldAmount));
                        }
                        break;

                    case QuestRewardType.Item:
                        if (!string.IsNullOrEmpty(reward.itemId))
                        {
                            logger?.Log(
                                $"[QuestSystem] Granting item '{reward.itemId}' to Player:{netId}.",
                                this
                            );
                            questRewardItemEvent?.Raise((netId, reward.itemId));
                        }
                        break;

                    case QuestRewardType.LootTable:
                        if (!string.IsNullOrEmpty(reward.lootTableId))
                            GrantLootTableReward(reward.lootTableId);
                        break;
                }
            }
        }

        private void GrantLootTableReward(string lootTableId)
        {
            var lootTable = ServerItemsRegistry.GetLootTableById(lootTableId);
            if (lootTable?.lootItems == null)
            {
                logger?.Log(
                    $"[QuestSystem] LootTable '{lootTableId}' not found or empty.",
                    this,
                    Logging.LogType.Warning
                );
                return;
            }

            foreach (var entry in lootTable.lootItems)
            {
                if (string.IsNullOrEmpty(entry.id))
                    continue;

                if (Random.Range(0, 100) < entry.dropProbability)
                {
                    logger?.Log(
                        $"[QuestSystem] LootTable '{lootTableId}' → item '{entry.id}' to Player:{netId}.",
                        this
                    );
                    questRewardItemEvent?.Raise((netId, entry.id));
                }
            }
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

    /// <summary>Runtime progress state for a single active quest. Server-side only.</summary>
    public sealed class QuestProgressState
    {
        public QuestData Quest { get; }
        public int Current { get; private set; }
        public int Target { get; }
        public bool IsCompleted => Current >= Target;

        public QuestProgressState(QuestData quest)
        {
            Quest = quest;
            Current = 0;
            Target = quest.type == QuestType.EnemySlays ? Mathf.Max(1, quest.targetAmount) : 1;
        }

        public void Increment() => Current = Mathf.Min(Current + 1, Target);
    }
}
