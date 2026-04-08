using System.Collections.Generic;
using System.Linq;
using Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Environment.Quest;
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
                this.enemySlayedEvent = ev1;
            if (resolver.TryResolve<NpcInteractedEvent>(out var ev2) && ev2 != null)
                this.npcInteractedEvent = ev2;
        }

        private EnemySlayedEvent enemySlayedEvent;
        private NpcInteractedEvent npcInteractedEvent;

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
                $"[QuestSystem] Player:{netId} accepted quest '{questData.title}' (type={questData.type}), target: {questData.targetId}, targetInterationId: {questData.targetInteractionId}.",
                this
            );

            // Lazy subscription: subscribe to the relevant channel only now.
            if (questData.type == QuestType.EnemySlays)
                SubscribeToEnemySlayed();
            else if (questData.type == QuestType.NpcInteract)
                SubscribeToNpcInteracted();

            SendProgressEvent(state);
        }

        private void OnEnemySlayed((uint killerNetId, string enemyTypeId) data)
        {
            logger?.Log(
                $"[QuestSystem] OnEnemySlayed received. killer={data.killerNetId}, enemyType={data.enemyTypeId}. PlayerNetId={netId}",
                this
            );

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
                    $"[QuestSystem] Player:{netId} progress on '{state.Quest.title}': "
                        + $"{state.Current}/{state.Target}",
                    this
                );

                SendProgressEvent(state);

                if (state.IsCompleted)
                    completedQuests.Add(state);
            }

            foreach (var state in completedQuests)
            {
                CompleteQuest(state);
            }

            if (anyUpdated && !HasActiveQuestsOfType(QuestType.EnemySlays))
                UnsubscribeFromEnemySlayed();
        }

        private void OnNpcInteracted((uint playerNetId, string npcId) data)
        {
            logger?.Log(
                $"[QuestSystem] OnNpcInteracted received. player={data.playerNetId}, npcId={data.npcId}. Me={netId}",
                this
            );

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
                    $"[QuestSystem] Player:{netId} interacted with target NPC '{data.npcId}' "
                        + $"for quest '{state.Quest.title}'.",
                    this
                );

                SendProgressEvent(state);

                if (state.IsCompleted)
                    completedQuests.Add(state);
            }

            foreach (var state in completedQuests)
            {
                CompleteQuest(state);
            }

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
            if (!connId.HasValue)
                return;

            worldMonitor.Events.Enqueue(
                new QuestCompletedEvent(
                    ownNetId,
                    connId.Value,
                    new QuestCompletedEventContent { QuestId = state.Quest.id }
                )
            );

            // TODO: grant rewards
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
            logger?.Log(
                $"[QuestSystem] Player:{netId} subscribed to EnemySlayedServerChannel.",
                this
            );
        }

        private void UnsubscribeFromEnemySlayed()
        {
            if (!subscribedToEnemySlayed || enemySlayedEvent == null)
                return;
            enemySlayedEvent.OnRaised -= OnEnemySlayed;
            subscribedToEnemySlayed = false;
            logger?.Log(
                $"[QuestSystem] Player:{netId} unsubscribed from EnemySlayedServerChannel.",
                this
            );
        }

        private void SubscribeToNpcInteracted()
        {
            if (subscribedToNpcInteracted || npcInteractedEvent == null)
                return;
            npcInteractedEvent.OnRaised += OnNpcInteracted;
            subscribedToNpcInteracted = true;
            logger?.Log(
                $"[QuestSystem] Player:{netId} subscribed to NpcInteractedServerChannel.",
                this
            );
        }

        private void UnsubscribeFromNpcInteracted()
        {
            if (!subscribedToNpcInteracted || npcInteractedEvent == null)
                return;
            npcInteractedEvent.OnRaised -= OnNpcInteracted;
            subscribedToNpcInteracted = false;
            logger?.Log(
                $"[QuestSystem] Player:{netId} unsubscribed from NpcInteractedServerChannel.",
                this
            );
        }
    }

    /// <summary>
    /// Kept server-side only — never serialized.
    /// </summary>
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
