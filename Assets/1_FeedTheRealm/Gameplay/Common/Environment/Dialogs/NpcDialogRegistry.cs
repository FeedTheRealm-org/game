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

            var messageById = new Dictionary<string, MessageData>();
            foreach (var dialog in dialogList)
            {
                if (dialog?.messages == null)
                    continue;
                foreach (var msg in dialog.messages)
                {
                    if (msg == null || string.IsNullOrEmpty(msg.id))
                        continue;
                    messageById[msg.id] = msg;
                }
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

                if (
                    npc.npcDialog == null
                    || npc.npcDialog.messageIds == null
                    || npc.npcDialog.messageIds.Count == 0
                )
                {
                    Debug.LogWarning(
                        $"[NpcDialogRegistry] NPC '{npc.id}' has no dialog data.",
                        this
                    );
                    _lookup[npc.id] = new List<MessageData>();
                    continue;
                }

                var messages = new List<MessageData>();
                foreach (var msgId in npc.npcDialog.messageIds)
                {
                    if (messageById.TryGetValue(msgId, out var msg))
                    {
                        if (string.IsNullOrEmpty(msg.Sender))
                            msg.Sender = npc.name;
                        messages.Add(msg);
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[NpcDialogRegistry] MessageId '{msgId}' not found for NPC '{npc.id}'.",
                            this
                        );
                    }
                }

                _lookup[npc.id] = messages;
            }

            Debug.Log($"[NpcDialogRegistry] Built lookup for {_lookup.Count} NPCs.", this);
        }
    }
}
