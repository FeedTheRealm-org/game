using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;

namespace FTR.Core.Server.Events
{
    public class InteractCompletedEvent : BaseServerEvent
    {
        public InteractCompletedEvent(uint netId, int targetConnectionId)
            : base(netId, targetConnectionId) { }

        public override ServerEventDTO ToDTO()
        {
            return new ServerEventDTO
            {
                Type = ServerEventType.InteractCompletedEvent,
                content = ByteString.Empty.ToByteArray(),
            };
        }
    }
}
