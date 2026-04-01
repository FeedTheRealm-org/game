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

        private Dictionary<string, List<MessageData>> _lookup;

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

            if (_lookup == null)
                BuildLookup(npcs, dialogs);

            return _lookup.TryGetValue(npcId, out messages);
        }

        public int GetMessageCount(string npcId)
        {
            if (TryGetMessages(npcId, out var messages))
                return messages.Count;
            return 0;
        }

        private void BuildLookup(List<NPCData> npcList, List<DialogData> dialogList)
        {
            _lookup = new Dictionary<string, List<MessageData>>();

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

                if (_lookup.ContainsKey(npc.id))
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
                    _lookup[npc.id] = new List<MessageData>();
                    continue;
                }

                if (!dialogById.TryGetValue(dialogId, out var dialogMatch))
                {
                    Debug.LogWarning(
                        $"[NpcDialogRegistry] NPC '{npc.id}' references dialogId '{dialogId}' which was not found in the dialog list.",
                        this
                    );
                    _lookup[npc.id] = new List<MessageData>();
                    continue;
                }

                if (dialogMatch.messages == null || dialogMatch.messages.Count == 0)
                {
                    Debug.LogWarning(
                        $"[NpcDialogRegistry] Dialog '{dialogId}' for NPC '{npc.id}' has no messages.",
                        this
                    );
                    _lookup[npc.id] = new List<MessageData>();
                    continue;
                }

                var messages = new List<MessageData>();
                foreach (var msg in dialogMatch.messages)
                {
                    if (msg == null)
                        continue;

                    if (string.IsNullOrEmpty(msg.sender))
                        msg.sender = npc.name;

                    messages.Add(msg);
                }

                _lookup[npc.id] = messages;
            }

            Debug.Log($"[NpcDialogRegistry] Built lookup for {_lookup.Count} NPCs.", this);
        }
    }
}
