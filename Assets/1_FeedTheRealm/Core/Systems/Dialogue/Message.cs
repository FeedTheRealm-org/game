using System;
using UnityEngine;

namespace Game.Core.Dialogue
{
    /// <summary>
    /// Represents a dialog message with a sender and content.
    /// </summary>
    [Serializable]
    public class Message
    {
        [SerializeField]
        private string _sender;

        [SerializeField]
        private string _content;

        public Message(string sender, string content)
        {
            _sender = sender;
            _content = content;
        }

        public string Sender
        {
            get => _sender;
        }

        public string Content
        {
            get => _content;
        }
    }
}
