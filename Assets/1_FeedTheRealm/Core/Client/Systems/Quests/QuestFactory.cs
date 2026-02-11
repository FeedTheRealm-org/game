using FTR.Core.Client.EventChannels;
using FTR.Core.Common.Enums;

namespace FTR.Core.Client.Quests
{
    public static class QuestFactory
    {
        public static Quest CreateQuest(
            QuestData questData,
            EnemySlayedEvent enemySlayedEvent,
            NpcInteractedEvent npcInteractedEvent,
            QuestProgressEvent questProgressEvent,
            QuestCompletedEvent questCompletedEvent
        )
        {
            switch (questData.Type)
            {
                case QuestType.EnemySlays:
                    return new SlayQuest(
                        questData,
                        enemySlayedEvent,
                        questProgressEvent,
                        questCompletedEvent
                    );
                case QuestType.NpcInteract:
                    return new InteractQuest(
                        questData,
                        npcInteractedEvent,
                        questProgressEvent,
                        questCompletedEvent
                    );
                default:
                    throw new System.Exception("Unsupported quest type: " + questData.Type);
            }
        }
    }
}
