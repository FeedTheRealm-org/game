using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;

namespace FTR.Core.Server.Events
{
    public class InteractFailedEvent : BaseServerEvent
    {
        public InteractFailedEvent(uint netId, int targetConnectionId)
            : base(netId, targetConnectionId) { }

        public override ServerEventDTO ToDTO()
        {
            return new ServerEventDTO
            {
                Type = ServerEventType.InteractFailedEvent,
                content = ByteString.Empty.ToByteArray(),
            };
        }
    }
}
