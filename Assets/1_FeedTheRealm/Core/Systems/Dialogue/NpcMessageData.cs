using System;
using Game.Core.Quests;
using Models;
using UnityEngine;

namespace Game.Core.Dialogue
{
    /// <summary>
    /// Represents an NPC dialog message with data, npc name and associated quest.
    /// </summary>
    [Serializable]
    public class NpcMessageData
    {
        [SerializeField]
        private MessageData _msg;

        [SerializeField]
        private QuestData _quest;

        public NpcMessageData(string content, QuestData quest)
        {
            _msg = new MessageData("???", "???", content);
            _quest = quest;
        }

        public MessageData Msg
        {
            get => _msg;
        }

        public QuestData Quest
        {
            get => _quest;
        }
    }
}
