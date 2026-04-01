using System.Collections.Generic;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Common.Environment.Dialogs
{
    [CreateAssetMenu(
        fileName = "NpcDialogRegistry",
        menuName = "Scriptable Objects/NpcDialogRegistry"
    )]
    public class NpcDialogRegistry : ScriptableObject
    {
        [SerializeField]
        private List<NPCData> npcs = new();

        [SerializeField]
        private List<DialogData> dialogs = new();

        private Dictionary<string, List<MessageData>> _messageLookup;

        private Dictionary<string, List<string>> _questLookup;

        private void OnEnable() => BuildLookup(npcs, dialogs);

        /// <summary>
        /// Replaces any previously built lookup with live data.
        /// </summary>
        public void Populate(List<NPCData> worldNpcs, List<DialogData> worldDialogs)
        {
            BuildLookup(worldNpcs, worldDialogs);
        }

        public bool TryGetMessages(string npcId, out List<MessageData> messages)
        {
            messages = null;

            if (string.IsNullOrEmpty(npcId))
            {
                Debug.LogWarning(
                    "[NpcDialogRegistry] TryGetMessages called with null or empty npcId."
                );
                return false;
            }

            if (_messageLookup == null)
                BuildLookup(npcs, dialogs);

            return _messageLookup.TryGetValue(npcId, out messages);
        }

        public int GetMessageCount(string npcId)
        {
            if (TryGetMessages(npcId, out var messages))
                return messages.Count;
            return 0;
        }

        /// <summary>
        /// Returns the questId associated with the message at the given index for this NPC.
        /// Returns empty string if no quest is associated with that message.
        /// </summary>
        public string GetQuestIdAt(string npcId, int messageIndex)
        {
            if (string.IsNullOrEmpty(npcId))
                return string.Empty;

            if (_questLookup == null)
                BuildLookup(npcs, dialogs);

            if (!_questLookup.TryGetValue(npcId, out var questIds))
                return string.Empty;

            if (messageIndex < 0 || messageIndex >= questIds.Count)
                return string.Empty;

            return questIds[messageIndex] ?? string.Empty;
        }

        private void BuildLookup(List<NPCData> npcList, List<DialogData> dialogList)
        {
            _messageLookup = new Dictionary<string, List<MessageData>>();
            _questLookup = new Dictionary<string, List<string>>();

            if (npcList == null || dialogList == null)
                return;

            var dialogById = new Dictionary<string, DialogData>();
            foreach (var dialog in dialogList)
            {
                if (dialog == null || string.IsNullOrEmpty(dialog.id))
                    continue;

                if (dialogById.ContainsKey(dialog.id))
                {
                    Debug.LogWarning(
                        $"[NpcDialogRegistry] Duplicate dialog id '{dialog.id}', skipping.",
                        this
                    );
                    continue;
                }

                dialogById[dialog.id] = dialog;
            }

            foreach (var npc in npcList)
            {
                if (npc == null || string.IsNullOrEmpty(npc.id))
                {
                    Debug.LogWarning(
                        "[NpcDialogRegistry] NPC entry with empty id, skipping.",
                        this
                    );
                    continue;
                }

                if (_messageLookup.ContainsKey(npc.id))
                {
                    Debug.LogWarning(
                        $"[NpcDialogRegistry] Duplicate NPC id '{npc.id}', skipping.",
                        this
                    );
                    continue;
                }

                var dialogId = npc.npcDialog?.dialogId;

                if (string.IsNullOrEmpty(dialogId))
                {
                    Debug.LogWarning(
                        $"[NpcDialogRegistry] NPC '{npc.id}' has no dialogId assigned.",
                        this
                    );
                    _messageLookup[npc.id] = new List<MessageData>();
                    _questLookup[npc.id] = new List<string>();
                    continue;
                }

                if (!dialogById.TryGetValue(dialogId, out var dialogMatch))
                {
                    Debug.LogWarning(
                        $"[NpcDialogRegistry] NPC '{npc.id}' references dialogId '{dialogId}' which was not found in the dialog list.",
                        this
                    );
                    _messageLookup[npc.id] = new List<MessageData>();
                    _questLookup[npc.id] = new List<string>();
                    continue;
                }

                if (dialogMatch.messages == null || dialogMatch.messages.Count == 0)
                {
                    Debug.LogWarning(
                        $"[NpcDialogRegistry] Dialog '{dialogId}' for NPC '{npc.id}' has no messages.",
                        this
                    );
                    _messageLookup[npc.id] = new List<MessageData>();
                    _questLookup[npc.id] = new List<string>();
                    continue;
                }

                var messageQuestMap =
                    npc.npcDialog?.GetMessageQuestMap() ?? new Dictionary<string, string>();

                var messages = new List<MessageData>();
                var questIds = new List<string>();

                foreach (var msg in dialogMatch.messages)
                {
                    if (msg == null)
                        continue;

                    if (string.IsNullOrEmpty(msg.Sender))
                        msg.Sender = npc.name;

                    messages.Add(msg);

                    messageQuestMap.TryGetValue(msg.id, out var questId);
                    questIds.Add(questId ?? string.Empty);
                }

                _messageLookup[npc.id] = messages;
                _questLookup[npc.id] = questIds;
            }

            Debug.Log($"[NpcDialogRegistry] Built lookup for {_messageLookup.Count} NPCs.", this);
        }
    }
}
