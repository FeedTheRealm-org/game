using System;
using Game.Core.Events;

namespace Game.Core.Quests
{
    public class Quest : IDisposable
    {
        /* Events */
        private EnemySlayedEvent enemySlayedEvent;
        private QuestProgressEvent questProgressEvent;
        private QuestCompletedEvent questCompletedEvent;

        /* Data */
        private QuestData questData;

        private QuestProgressData questProgressData;

        private int currentSlayedCount = 0;

        public Quest(
            QuestData questData,
            EnemySlayedEvent enemySlayedEvent,
            QuestProgressEvent questProgressEvent,
            QuestCompletedEvent questCompletedEvent
        )
        {
            this.enemySlayedEvent = enemySlayedEvent;
            this.questProgressEvent = questProgressEvent;
            this.questCompletedEvent = questCompletedEvent;
            this.questData = questData;

            this.questProgressData = new QuestProgressData
            {
                Id = questData.Id,
                TargetAmount = questData.TargetAmount,
                CurrentAmount = currentSlayedCount,
                Quest = questData,
            };
        }

        public void Start()
        {
            enemySlayedEvent.OnRaised += OnEnemySlayed;
            RaiseProgress();
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

            RaiseProgress();
        }

        private void RaiseProgress()
        {
            questProgressData.CurrentAmount = currentSlayedCount;
            questProgressEvent.Raise(questProgressData);
        }
    }
}
