using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;

namespace FTR.Core.Server.Events
{
    public class LootbagPickedUpEvent : BaseServerEvent
    {
        public LootbagPickedUpEvent(uint netId, int targetConnectionId)
            : base(netId, targetConnectionId) { }

        public override ServerEventDTO ToDTO()
        {
            return new ServerEventDTO
            {
                Type = ServerEventType.LootbagPickedUpEvent,
                content = ByteString.Empty.ToByteArray(),
            };
        }
    }
}
