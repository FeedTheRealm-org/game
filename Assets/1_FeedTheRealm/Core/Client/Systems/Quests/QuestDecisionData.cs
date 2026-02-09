namespace Game.Core.Client.Quests
{
    public class QuestDecisionData
    {
        public QuestData Quest;

        public bool IsAccepted;

        public QuestDecisionData(QuestData questData, bool isAccepted)
        {
            Quest = questData;
            IsAccepted = isAccepted;
        }
    }
}
