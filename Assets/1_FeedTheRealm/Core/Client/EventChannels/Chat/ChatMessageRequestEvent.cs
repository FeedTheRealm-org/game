using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Chat
{
    [CreateAssetMenu(menuName = "Events/Client/Chat/ChatMessageRequestEvent")]
    public class ChatMessageRequestEvent : EventChannelSO<string> { }
}
