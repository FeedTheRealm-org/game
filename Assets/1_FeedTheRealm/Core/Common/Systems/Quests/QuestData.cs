using System;
using FTR.Core.Common.Enums;
using UnityEngine;

namespace FTR.Core.Common.Quests
{
    [Serializable]
    public class QuestData
    {
        [SerializeField]
        public string Id;

        [SerializeField]
        public string Title;

        [SerializeField]
        public string Content;

        [SerializeField]
        public int TargetAmount;

        [SerializeField]
        public string TargetInteractionId;

        [SerializeField]
        public QuestType Type;

        // TODO: add a type enum or a condition abstract class, and reward system
    }
}
