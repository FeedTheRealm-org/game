using System.Collections.Generic;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Client.Environment.Quest
{
    /// <summary>
    /// Client-side ScriptableObject that stores full QuestData from world data.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ClientQuestRegistry",
        menuName = "Scriptable Objects/ClientQuestRegistry"
    )]
    public class ClientQuestRegistry : ScriptableObject
    {
        private Dictionary<string, QuestData> _questById;

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

        public bool TryGetQuest(string questId, out QuestData questData)
        {
            questData = null;

            if (_questById == null || string.IsNullOrEmpty(questId))
                return false;

            return _questById.TryGetValue(questId, out questData);
        }
    }
}
