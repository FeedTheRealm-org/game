using System;
using UnityEngine;

namespace Game.Core.Quests
{
    public class QuestDecisionData
    {
        private QuestData _questData;

        private bool _isAccepted;

        public QuestDecisionData(QuestData questData, bool isAccepted)
        {
            _questData = questData;
            _isAccepted = isAccepted;
        }

        public QuestData QuestData => _questData;

        public bool IsAccepted => _isAccepted;
    }
}
