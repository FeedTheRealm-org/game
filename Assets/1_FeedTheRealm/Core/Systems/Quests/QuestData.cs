using System;
using UnityEngine;

namespace Game.Core.Quests
{
    [Serializable]
    public class QuestData
    {
        [SerializeField]
        private string _id;

        [SerializeField]
        private string _sender;

        [SerializeField]
        private string _title;

        [SerializeField]
        private string _content;

        public QuestData(string id, string sender, string title, string content)
        {
            _id = id;
            _sender = sender;
            _title = title;
            _content = content;
        }

        public string Id
        {
            get => _id;
        }

        public string Sender
        {
            get => _sender;
        }

        public string Title
        {
            get => _title;
        }

        public string Content
        {
            get => _content;
        }
    }
}
