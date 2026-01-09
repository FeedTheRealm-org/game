using System.Collections.Generic;
using Game.Core.Events;
using UnityEngine;

namespace Game.Core.Quests
{
    public class QuestManager : ScriptableObject
    {
        [Header("Events")]
        [SerializeField]
        private QuestDecisionEvent questDecisionEvent;

        [SerializeField]
        private QuestCompletedEvent questCompletedEvent;

        [SerializeField]
        private EnemySlayedEvent enemySlayedEvent;

        [Header("General Settings")]
        [SerializeField]
        private Logging.Logger logger;

        private List<Quest> activeQuests = new List<Quest>();

        private void OnEnable()
        {
            questDecisionEvent.OnRaised += OnQuestDecision;
        }

        private void OnDisable()
        {
            questDecisionEvent.OnRaised -= OnQuestDecision;
            foreach (var quest in activeQuests)
            {
                quest.Dispose();
            }
        }

        private void OnQuestDecision(QuestDecisionData decisionData)
        {
            if (decisionData.IsAccepted)
            {
                var newQuest = new Quest(decisionData.Quest, enemySlayedEvent, questCompletedEvent);
                newQuest.Start();
                activeQuests.Add(newQuest);
                logger.Log($"Quest '{decisionData.Quest.Title}' accepted & started.", this);
            }
        }
    }
}
