using FTRShared.Runtime.Models;

namespace FTR.Core.Common.Quests
{
    public class QuestProgressData
    {
        public string Id;

        public int TargetProgressAmount;

        public int CurrentProgressAmount;

        public QuestData Quest;
    }
}
