using Enums;
using FTR.Core.Common.Enums;
using FTR.Core.Common.EventChannels;
using FTRShared.Runtime.Models;

namespace FTR.Core.Common.Quests
{
    public static class QuestFactory
    {
        public static Quest CreateQuest(
            QuestData questData,
            EnemySlayedEvent enemySlayedEvent,
            NpcInteractedEvent npcInteractedEvent,
            QuestProgressEvent questProgressEvent,
            QuestCompletedEvent questCompletedEvent,
            string effectiveId
        )
        {
            switch (questData.type)
            {
                case QuestType.EnemySlays:
                    return new SlayQuest(
                        questData,
                        enemySlayedEvent,
                        questProgressEvent,
                        questCompletedEvent,
                        effectiveId
                    );
                case QuestType.NpcInteract:
                    return new InteractQuest(
                        questData,
                        npcInteractedEvent,
                        questProgressEvent,
                        questCompletedEvent,
                        effectiveId
                    );
                default:
                    throw new System.Exception("Unsupported quest type: " + questData.type);
            }
        }
    }
}
