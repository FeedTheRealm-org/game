using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

namespace FTR.Core.Common.EventChannels
{
    // ReceivedActionCommandEvent is an event treated as a mpsc channel
    // Do not subscribe more than one method to it! (Only server-side NetworkService)
    [CreateAssetMenu(menuName = "Events/NetworkEvents/Received Action Command")]
    public class ReceivedActionCommandEvent : EventChannelSO<ActionCommandDTO> { }
}
