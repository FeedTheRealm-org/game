using Enums;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Utils
{
    /// <summary>Runtime progress state for a single active quest. Server-side only.</summary>
    public sealed class QuestProgressState
    {
        public QuestData Quest { get; }
        public string NpcId { get; }
        public string EffectiveQuestId =>
            string.IsNullOrEmpty(NpcId) ? Quest.id : $"{Quest.id}_{NpcId}";
        public int Current { get; private set; }
        public int Target { get; }
        public bool IsCompleted => Current >= Target;

        public QuestProgressState(QuestData quest, string npcId = "")
        {
            Quest = quest;
            NpcId = npcId;
            Current = 0;
            Target = quest.type == QuestType.EnemySlays ? Mathf.Max(1, quest.targetAmount) : 1;
        }

        public void Increment() => Current = Mathf.Min(Current + 1, Target);
    }
}
