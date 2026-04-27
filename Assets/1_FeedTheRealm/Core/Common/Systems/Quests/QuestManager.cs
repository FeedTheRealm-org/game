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
                var newQuest = QuestFactory.CreateQuest(
                    decisionData.Quest,
                    enemySlayedEvent,
                    npcInteractedEvent,
                    questProgressEvent,
                    questCompletedEvent
                );
                newQuest.Start();
                activeQuests.Add(decisionData.Quest.id, newQuest);
                logger.Log($"Quest '{decisionData.Quest.title}' accepted & started.", this);
            }
        }

        private void OnQuestCompleted(QuestData questData)
        {
            if (activeQuests.ContainsKey(questData.id))
            {
                activeQuests[questData.id].Dispose();
                activeQuests.Remove(questData.id);
                logger.Log(
                    $"Quest '{questData.title}' completed and removed from active quests.",
                    this
                );
            }
        }
    }
}
