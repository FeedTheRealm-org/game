using System.Collections.Generic;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Common.Environment.Dialogs
{
    /// <summary>
    /// Server usage: queries are always scoped to a (npcId, progressionIndex) pair so
    /// NpcInteractSystem can serve the correct dialog based on each player's quest progress.
    ///
    /// Client usage: queries are scoped to a dialogId so InteractView can look up the exact
    /// MessageData list for any dialog the server tells it to display
    /// </summary>
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

        /// <summary>
        /// npcId → ordered list of ProgressionEntry, one per NPCDialogData slot.
        /// </summary>
        private Dictionary<string, List<ProgressionEntry>> _progressionLookup;

        /// <summary>
        /// dialogId → flat list of MessageData (used by the client to display any dialog).
        /// </summary>
        private Dictionary<string, List<MessageData>> _dialogMessageLookup;

        /// <summary>npcId → npc name (for sender fallback).</summary>
        private Dictionary<string, string> _npcNameLookup;

        /// <summary>
        /// Represents a single slot in an NPC's dialog progression.
        /// </summary>
        private class ProgressionEntry
        {
            public string DialogId;
            public List<string> QuestIds = new();
            public string OnQuestAcceptedDialogId = "";

            /// <summary>
            /// When true the quest in this step is repeatable and this progression slot never
            /// advances to the next one. The cooldown value is stored for future use.
            /// </summary>
            public bool IsRepeatable;
            public string RepeatableCooldown;
        }

        private void OnEnable() => BuildLookup(npcs, dialogs);

        /// <summary>
        /// Replaces any previously built lookup with live data. Call after the world loads.
        /// </summary>
        public void Populate(List<NPCData> worldNpcs, List<DialogData> worldDialogs)
        {
            npcs = worldNpcs ?? new List<NPCData>();
            dialogs = worldDialogs ?? new List<DialogData>();
            BuildLookup(npcs, dialogs);
        }

        public int GetProgressionCount(string npcId)
        {
            if (!TryGetProgression(npcId, out var list))
                return 0;
            return list.Count;
        }

        public int GetMessageCount(string npcId, int progressionIndex = 0)
        {
            if (!TryGetEntry(npcId, progressionIndex, out var entry))
                return 0;

            if (!TryGetDialogMessages(entry.DialogId, out var messages))
                return 0;

            return messages.Count;
        }

        public string GetDialogId(string npcId, int progressionIndex)
        {
            if (!TryGetEntry(npcId, progressionIndex, out var entry))
                return string.Empty;
            return entry.DialogId;
        }

        public string GetQuestIdAt(string npcId, int progressionIndex, int messageIndex)
        {
            if (!TryGetEntry(npcId, progressionIndex, out var entry))
                return string.Empty;

            if (messageIndex < 0 || messageIndex >= entry.QuestIds.Count)
                return string.Empty;

            return entry.QuestIds[messageIndex] ?? string.Empty;
        }

        public string GetOnQuestAcceptedDialogId(string npcId, int progressionIndex)
        {
            if (!TryGetEntry(npcId, progressionIndex, out var entry))
                return string.Empty;
            return entry.OnQuestAcceptedDialogId;
        }

        public bool IsRepeatableAt(string npcId, int progressionIndex)
        {
            if (!TryGetEntry(npcId, progressionIndex, out var entry))
                return false;
            return entry.IsRepeatable;
        }

        public bool TryGetMessagesByDialogId(string dialogId, out List<MessageData> messages)
        {
            messages = null;

            if (string.IsNullOrEmpty(dialogId))
                return false;

            if (_dialogMessageLookup == null)
                BuildLookup(npcs, dialogs);

            return _dialogMessageLookup.TryGetValue(dialogId, out messages);
        }

        public bool TryGetNpcName(string npcId, out string npcName)
        {
            npcName = string.Empty;

            if (string.IsNullOrEmpty(npcId) || _npcNameLookup == null)
                return false;

            return _npcNameLookup.TryGetValue(npcId, out npcName);
        }

        private bool TryGetProgression(string npcId, out List<ProgressionEntry> list)
        {
            list = null;

            if (string.IsNullOrEmpty(npcId))
                return false;

            if (_progressionLookup == null)
                BuildLookup(npcs, dialogs);

            return _progressionLookup.TryGetValue(npcId, out list);
        }

        private bool TryGetEntry(string npcId, int index, out ProgressionEntry entry)
        {
            entry = null;

            if (!TryGetProgression(npcId, out var list))
                return false;

            if (index < 0 || index >= list.Count)
                return false;

            entry = list[index];
            return true;
        }

        private bool TryGetDialogMessages(string dialogId, out List<MessageData> messages)
        {
            messages = null;

            if (string.IsNullOrEmpty(dialogId) || _dialogMessageLookup == null)
                return false;

            return _dialogMessageLookup.TryGetValue(dialogId, out messages);
        }

        private void BuildLookup(List<NPCData> npcList, List<DialogData> dialogList)
        {
            _progressionLookup = new Dictionary<string, List<ProgressionEntry>>();
            _dialogMessageLookup = new Dictionary<string, List<MessageData>>();
            _npcNameLookup = new Dictionary<string, string>();

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

            foreach (var kvp in dialogById)
            {
                var msgList = new List<MessageData>();
                if (kvp.Value.messages != null)
                {
                    foreach (var msg in kvp.Value.messages)
                    {
                        if (msg != null)
                            msgList.Add(msg);
                    }
                }
                _dialogMessageLookup[kvp.Key] = msgList;
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

                if (_progressionLookup.ContainsKey(npc.id))
                {
                    Debug.LogWarning(
                        $"[NpcDialogRegistry] Duplicate NPC id '{npc.id}', skipping.",
                        this
                    );
                    continue;
                }

                _npcNameLookup[npc.id] = npc.name ?? string.Empty;

                var progression = new List<ProgressionEntry>();

                if (npc.dialogProgression == null || npc.dialogProgression.Count == 0)
                {
                    Debug.LogWarning(
                        $"[NpcDialogRegistry] NPC '{npc.id}' has no dialogProgression.",
                        this
                    );
                    _progressionLookup[npc.id] = progression;
                    continue;
                }

                foreach (var slot in npc.dialogProgression)
                {
                    if (slot == null || string.IsNullOrEmpty(slot.dialogId))
                    {
                        Debug.LogWarning(
                            $"[NpcDialogRegistry] NPC '{npc.id}' has a progression slot with empty dialogId, skipping slot.",
                            this
                        );
                        continue;
                    }

                    if (!dialogById.TryGetValue(slot.dialogId, out var dialogData))
                    {
                        Debug.LogWarning(
                            $"[NpcDialogRegistry] NPC '{npc.id}' progression references unknown dialogId '{slot.dialogId}', skipping slot.",
                            this
                        );
                        continue;
                    }

                    var questMap = slot.GetMessageQuestMap();
                    var questIds = new List<string>();

                    if (dialogData.messages != null)
                    {
                        foreach (var msg in dialogData.messages)
                        {
                            if (msg == null)
                                continue;

                            if (string.IsNullOrEmpty(msg.sender))
                                msg.sender = npc.name;

                            questMap.TryGetValue(msg.id, out var qId);
                            questIds.Add(qId ?? string.Empty);
                        }
                    }

                    progression.Add(
                        new ProgressionEntry
                        {
                            DialogId = slot.dialogId,
                            QuestIds = questIds,
                            OnQuestAcceptedDialogId = slot.onQuestAcceptedDialogId ?? string.Empty,
                            IsRepeatable = slot.IsRepeatable,
                            RepeatableCooldown = slot.repeatableQuestCooldown ?? string.Empty,
                        }
                    );
                }

                _progressionLookup[npc.id] = progression;
            }

            Debug.Log(
                $"[NpcDialogRegistry] Built lookup for {_progressionLookup.Count} NPCs, {_dialogMessageLookup.Count} dialogs.",
                this
            );
        }
    }
}
