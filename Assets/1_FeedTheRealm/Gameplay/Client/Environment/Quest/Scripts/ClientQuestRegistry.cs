using System.Collections.Generic;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Client.Environment.Quest
{
    /// <summary>
    /// Client-side ScriptableObject that stores full QuestData from world data.
    /// Used by QuestView to resolve QuestData from a questId received in a DialogEvent.
    /// Populated by ClientQuestLoader at world load time.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ClientQuestRegistry",
        menuName = "Scriptable Objects/ClientQuestRegistry"
    )]
    public class ClientQuestRegistry : ScriptableObject
    {
        private Dictionary<string, QuestData> _questById;

        /// <summary>
        /// Rebuilds the registry from world quest data.
        /// </summary>
        public void Populate(List<QuestData> worldQuests)
        {
            _questById = new Dictionary<string, QuestData>();

            if (worldQuests == null)
                return;

            foreach (var quest in worldQuests)
            {
                if (quest == null || string.IsNullOrEmpty(quest.id))
                    continue;

                if (_questById.ContainsKey(quest.id))
                {
                    Debug.LogWarning(
                        $"[ClientQuestRegistry] Duplicate quest id '{quest.id}', skipping."
                    );
                    continue;
                }

                _questById[quest.id] = quest;
            }

            Debug.Log($"[ClientQuestRegistry] Populated with {_questById.Count} quest(s).");
        }

        /// <summary>
        /// Returns the QuestData for the given questId, or null if not found.
        /// </summary>
        public bool TryGetQuest(string questId, out QuestData questData)
        {
            questData = null;

            if (_questById == null || string.IsNullOrEmpty(questId))
                return false;

            return _questById.TryGetValue(questId, out questData);
        }
    }
}
