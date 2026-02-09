using System;
using Game.Core.Client.Enum;
using UnityEngine;

namespace Game.Core.Client.Quests
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
