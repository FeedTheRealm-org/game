using System.Collections.Generic;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Environment.Quest
{
    /// <summary>
    /// Server-side registry that stores full QuestData from world data.
    /// Used by QuestSystem to validate AcceptQuest commands and retrieve
    /// quest conditions (type, targetId, targetAmount, targetInteractionId).
    /// </summary>
    [CreateAssetMenu(
        fileName = "ServerQuestRegistry",
        menuName = "Scriptable Objects/ServerQuestRegistry"
    )]
    public class ServerQuestRegistry : ScriptableObject
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
                        $"[ServerQuestRegistry] Duplicate quest id '{quest.id}', skipping."
                    );
                    continue;
                }

                _questById[quest.id] = quest;
                Debug.Log(
                    $"[ServerQuestRegistry] Registered quest '{quest.id}' of type '{quest.type}'."
                );
            }

            Debug.Log($"[ServerQuestRegistry] Populated with {_questById.Count} quest(s).");
        }

        public bool IsValidQuestId(string questId)
        {
            if (_questById == null || string.IsNullOrEmpty(questId))
                return false;

            return _questById.ContainsKey(questId);
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
