using FTR.Core.Common.EventChannels;
using UnityEngine;

namespace FTR.Core.Client.EventChannels.Chat
{
    [CreateAssetMenu(menuName = "Events/PlayerEvents/Chat Toggle")]
    public class ChatToggleEvent : EventChannelSO<bool> { }
}
