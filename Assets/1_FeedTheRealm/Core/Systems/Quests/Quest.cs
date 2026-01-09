using System;
using Game.Core.Events;

namespace Game.Core.Quests
{
    public class Quest : IDisposable
    {
        /* Events */
        private EnemySlayedEvent enemySlayedEvent;
        private QuestCompletedEvent questCompletedEvent;

        /* Data */
        private QuestData questData;

        private int currentSlayedCount = 0;

        public Quest(
            QuestData questData,
            EnemySlayedEvent enemySlayedEvent,
            QuestCompletedEvent questCompletedEvent
        )
        {
            this.enemySlayedEvent = enemySlayedEvent;
            this.questCompletedEvent = questCompletedEvent;
            this.questData = questData;
        }

        public void Start()
        {
            enemySlayedEvent.OnRaised += OnEnemySlayed;
        }

        public void Dispose()
        {
            enemySlayedEvent.OnRaised -= OnEnemySlayed;
        }

        private void OnEnemySlayed()
        {
            currentSlayedCount++;
            if (currentSlayedCount >= questData.TargetAmount)
            {
                questCompletedEvent.Raise(questData);
                Dispose();
            }
        }
    }
}
