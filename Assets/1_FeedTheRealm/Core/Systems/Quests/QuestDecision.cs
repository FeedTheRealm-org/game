using System;
using UnityEngine;

namespace Game.Core.Quests
{
    public class QuestDecision
    {
        private QuestData _questData;

        private bool _isAccepted;

        public QuestDecision(QuestData questData, bool isAccepted)
        {
            _questData = questData;
            _isAccepted = isAccepted;
        }

        public QuestData QuestData => _questData;

        public bool IsAccepted => _isAccepted;
    }
}
