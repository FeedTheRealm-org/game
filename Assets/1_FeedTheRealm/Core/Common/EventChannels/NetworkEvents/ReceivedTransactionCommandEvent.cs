using FTR.Core.Common.Protocol.RpcMessages;
using UnityEngine;

namespace FTR.Core.Common.EventChannels
{
    // ReceivedTransactionCommandEvent is an event treated as a mpsc channel
    // Do not subscribe more than one method to it! (Only server-side NetworkService)
    [CreateAssetMenu(menuName = "Events/NetworkEvents/Received Transaction Command")]
    public class ReceivedTransactionCommandEvent : EventChannelSO<TransactionCommandDTO> { }
}
