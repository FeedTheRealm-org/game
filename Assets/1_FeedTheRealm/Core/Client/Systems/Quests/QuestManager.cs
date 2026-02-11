using System.Collections.Generic;
using FTR.Core.Client.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.Quests
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
                $"QUEST MANAGER: Quest decision received for quest '{decisionData.Quest.Title}'.",
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
                activeQuests.Add(decisionData.Quest.Id, newQuest);
                logger.Log($"Quest '{decisionData.Quest.Title}' accepted & started.", this);
            }
        }

        private void OnQuestCompleted(QuestData questData)
        {
            if (activeQuests.ContainsKey(questData.Id))
            {
                activeQuests[questData.Id].Dispose();
                activeQuests.Remove(questData.Id);
                logger.Log(
                    $"Quest '{questData.Title}' completed and removed from active quests.",
                    this
                );
            }
        }
    }
}
