using System.Collections.Generic;
using Game.Core.Events;
using UnityEngine;

namespace Game.Core.Quests
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

        [Header("General Settings")]
        [SerializeField]
        private Logging.Logger logger;

        // TODO: grows indefinitely FIX (remove completed quests)
        private List<Quest> activeQuests = new List<Quest>();

        private void OnEnable()
        {
            questDecisionEvent.OnRaised += OnQuestDecision;
            logger.Log("QUEST MANAGER: Quest Manager enabled.", this);
        }

        private void OnDisable()
        {
            questDecisionEvent.OnRaised -= OnQuestDecision;
            foreach (var quest in activeQuests)
            {
                quest.Dispose();
            }
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
                var newQuest = new Quest(
                    decisionData.Quest,
                    enemySlayedEvent,
                    questProgressEvent,
                    questCompletedEvent
                );
                newQuest.Start();
                activeQuests.Add(newQuest);
                logger.Log($"Quest '{decisionData.Quest.Title}' accepted & started.", this);
            }
        }
    }
}
