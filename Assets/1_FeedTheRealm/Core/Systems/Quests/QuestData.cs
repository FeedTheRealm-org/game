using System;
using UnityEngine;

namespace Game.Core.Quests
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

        // TODO: add a type enum or a condition abstract class, and reward system
    }
}
