using System.Collections.Generic;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Environment.Quest
{
    /// <summary>
    /// Lightweight registry that stores only the set of valid quest ids from world data.
    /// Used server-side to validate AcceptQuest commands without holding full QuestData in memory.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ServerQuestRegistry",
        menuName = "Scriptable Objects/ServerQuestRegistry"
    )]
    public class ServerQuestRegistry : ScriptableObject
    {
        private HashSet<string> _validQuestIds;

        public void Populate(List<QuestData> worldQuests)
        {
            _validQuestIds = new HashSet<string>();

            if (worldQuests == null)
                return;

            foreach (var quest in worldQuests)
            {
                if (quest == null || string.IsNullOrEmpty(quest.id))
                    continue;
                _validQuestIds.Add(quest.id);
            }

            Debug.Log($"Populated with {_validQuestIds.Count} quest(s).");
        }

        public bool IsValidQuestId(string questId)
        {
            if (_validQuestIds == null || string.IsNullOrEmpty(questId))
                return false;

            return _validQuestIds.Contains(questId);
        }
    }
}
