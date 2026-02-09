using Game.Core.RpcMessages;
using UnityEngine;

namespace Game.Core.Events
{
    // ReceivedActionCommandEvent is an event treated as a mpsc channel
    // Do not subscribe more than one method to it! (Only server-side NetworkService)
    [CreateAssetMenu(menuName = "Events/NetworkEvents/Received Action Command")]
    public class ReceivedActionCommandEvent : EventChannelSO<ActionCommandDTO> { }
}
