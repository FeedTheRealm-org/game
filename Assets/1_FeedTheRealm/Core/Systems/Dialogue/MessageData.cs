using System;
using UnityEngine;

namespace Game.Core.Dialogue
{
    /// <summary>
    /// Represents a dialog message with a sender and content.
    /// </summary>
    [Serializable]
    public class MessageData
    {
        [SerializeField]
        private string _sender;

        [SerializeField]
        private string _content;

        public MessageData(string sender, string content)
        {
            _sender = sender;
            _content = content;
        }

        public string Sender
        {
            get => _sender;
            set => _sender = value;
        }

        public string Content
        {
            get => _content;
        }
    }
}
