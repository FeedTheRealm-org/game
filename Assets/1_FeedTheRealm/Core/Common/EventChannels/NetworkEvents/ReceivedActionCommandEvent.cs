using Game.Core.Common.RpcMessages;
using UnityEngine;

namespace Game.Core.Common.Events
{
    // ReceivedActionCommandEvent is an event treated as a mpsc channel
    // Do not subscribe more than one method to it! (Only server-side NetworkService)
    [CreateAssetMenu(menuName = "Events/NetworkEvents/Received Action Command")]
    public class ReceivedActionCommandEvent : EventChannelSO<ActionCommandDTO> { }
}
