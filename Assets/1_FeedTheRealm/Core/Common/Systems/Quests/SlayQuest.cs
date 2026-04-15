using FTR.Core.Common.EventChannels;
using FTRShared.Runtime.Models;

namespace FTR.Core.Common.Quests
{
    public class SlayQuest : Quest
    {
        private string effectiveId;

        /* Events */
        private EnemySlayedEvent enemySlayedEvent;
        private QuestProgressEvent questProgressEvent;
        private QuestCompletedEvent questCompletedEvent;

        /* Data */
        private readonly QuestData questData;

        private QuestProgressData questProgressData;

        private int currentSlayedCount = 0;

        public SlayQuest(
            QuestData questData,
            EnemySlayedEvent enemySlayedEvent,
            QuestProgressEvent questProgressEvent,
            QuestCompletedEvent questCompletedEvent,
            string effectiveId
        )
        {
            this.effectiveId = effectiveId;
            this.enemySlayedEvent = enemySlayedEvent;
            this.questProgressEvent = questProgressEvent;
            this.questCompletedEvent = questCompletedEvent;
            this.questData = questData;

            this.questProgressData = new QuestProgressData
            {
                Id = effectiveId,
                TargetProgressAmount = questData.targetAmount,
                CurrentProgressAmount = currentSlayedCount,
                Quest = questData,
            };
        }

        public override void Start()
        {
            enemySlayedEvent.OnRaised += OnEnemySlayed;
            RaiseProgress();
        }

        public override void Dispose()
        {
            enemySlayedEvent.OnRaised -= OnEnemySlayed;
        }

        private void OnEnemySlayed()
        {
            if (currentSlayedCount >= questData.targetAmount)
                return;

            currentSlayedCount++;

            RaiseProgress();

            if (currentSlayedCount >= questData.targetAmount)
            {
                questCompletedEvent.Raise((questData, effectiveId));
                Dispose();
            }
        }

        private void RaiseProgress()
        {
            questProgressData.CurrentProgressAmount = currentSlayedCount;
            questProgressEvent.Raise(questProgressData);
        }
    }
}
