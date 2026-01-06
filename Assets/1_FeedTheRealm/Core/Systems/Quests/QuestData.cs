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
        private string _title;

        [SerializeField]
        private string _content;

        public QuestData(string id, string title, string content)
        {
            _id = id;
            _title = title;
            _content = content;
        }

        public string Id
        {
            get => _id;
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
