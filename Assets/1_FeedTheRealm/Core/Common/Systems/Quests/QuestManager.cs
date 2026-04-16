using System.Collections.Generic;
using FTR.Core.Common.EventChannels;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Core.Common.Quests
{
    /// <summary>
    /// Manages active quests, handling quest acceptance and completion.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        [Header("Events")]
        [SerializeField]
        private QuestDecisionEvent questDecisionEvent;

        [SerializeField]
        private QuestProgressEvent questProgressEvent;

        [SerializeField]
        private QuestCompletedEvent questCompletedEvent;

        [SerializeField]
        private EnemySlayedEvent enemySlayedEvent;

        [SerializeField]
        private NpcInteractedEvent npcInteractedEvent;

        [Header("General Settings")]
        [SerializeField]
        private Logging.Logger logger;

        private readonly Dictionary<string, Quest> activeQuests = new Dictionary<string, Quest>();

        private void OnEnable()
        {
            questDecisionEvent.OnRaised += OnQuestDecision;
            questCompletedEvent.OnRaised += OnQuestCompleted;
            logger.Log("QUEST MANAGER: Quest Manager enabled.", this);
        }

        private void OnDisable()
        {
            questDecisionEvent.OnRaised -= OnQuestDecision;
            questCompletedEvent.OnRaised -= OnQuestCompleted;
            foreach (var quest in activeQuests)
            {
                quest.Value.Dispose();
            }
            activeQuests.Clear();
            logger.Log("QUEST MANAGER: Quest Manager disabled.", this);
        }

        private void OnQuestDecision(QuestDecisionData decisionData)
        {
            logger.Log(
                $"QUEST MANAGER: Quest decision received for quest '{decisionData.Quest.title}'.",
                this
            );
            if (decisionData.IsAccepted)
            {
                string effectiveId = string.IsNullOrEmpty(decisionData.NpcId)
                    ? decisionData.Quest.id
                    : $"{decisionData.Quest.id}_{decisionData.NpcId}";
                var newQuest = QuestFactory.CreateQuest(
                    decisionData.Quest,
                    enemySlayedEvent,
                    npcInteractedEvent,
                    questProgressEvent,
                    questCompletedEvent,
                    effectiveId
                );
                newQuest.Start();
                activeQuests.Add(effectiveId, newQuest);
                logger.Log(
                    $"Quest '{decisionData.Quest.title}' (Effective: {effectiveId}) accepted & started.",
                    this
                );
            }
        }

        private void OnQuestCompleted((QuestData Quest, string EffectiveId) payload)
        {
            if (activeQuests.TryGetValue(payload.EffectiveId, out var quest))
            {
                logger.Log(
                    $"QUEST MANAGER: Quest '{payload.Quest.title}' (Effective: {payload.EffectiveId}) completed.",
                    this
                );
                quest.Dispose();
                activeQuests.Remove(payload.EffectiveId);
            }
            else
            {
                logger.Log(
                    $"QUEST MANAGER: Completed quest '{payload.Quest.title}' but effective ID {payload.EffectiveId} was not active.",
                    this
                );
            }
        }
    }
}
