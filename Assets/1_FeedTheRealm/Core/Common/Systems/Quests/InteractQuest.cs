using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Interactions;

namespace FTR.Core.Common.Quests
{
    public class InteractQuest : Quest
    {
        /* Events */
        private NpcInteractedEvent npcInteractedEvent;
        private QuestProgressEvent questProgressEvent;
        private QuestCompletedEvent questCompletedEvent;

        /* Data */
        private readonly QuestData questData;

        private QuestProgressData questProgressData;

        private bool interactedWithTarget = false;
        private int targetProgressAmount = 1;

        public InteractQuest(
            QuestData questData,
            NpcInteractedEvent npcInteractedEvent,
            QuestProgressEvent questProgressEvent,
            QuestCompletedEvent questCompletedEvent
        )
        {
            this.npcInteractedEvent = npcInteractedEvent;
            this.questProgressEvent = questProgressEvent;
            this.questCompletedEvent = questCompletedEvent;
            this.questData = questData;

            this.questProgressData = new QuestProgressData
            {
                Id = questData.Id,
                TargetProgressAmount = targetProgressAmount,
                CurrentProgressAmount = 0,
                Quest = questData,
            };
        }

        public override void Start()
        {
            npcInteractedEvent.OnRaised += OnNpcInteracted;
            RaiseProgress();
        }

        public override void Dispose()
        {
            npcInteractedEvent.OnRaised -= OnNpcInteracted;
        }

        private void OnNpcInteracted(NpcInteractedData interactedData)
        {
            if (interactedData.NpcId != questData.TargetInteractionId || interactedWithTarget)
                return;

            interactedWithTarget = true;

            questCompletedEvent.Raise(questData);
            RaiseProgress();
            Dispose();
        }

        private void RaiseProgress()
        {
            questProgressData.CurrentProgressAmount = interactedWithTarget
                ? targetProgressAmount
                : 0;
            questProgressEvent.Raise(questProgressData);
        }
    }
}
