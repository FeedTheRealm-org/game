using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;

namespace FTR.Core.Server.Events
{
    public class QuestProgressEvent : BaseServerEvent
    {
        private readonly QuestProgressEventContent content;

        public QuestProgressEvent(
            uint netId,
            int targetConnectionId,
            QuestProgressEventContent content
        )
            : base(netId, targetConnectionId)
        {
            this.content = content;
        }

        public override ServerEventDTO ToDTO()
        {
            return new ServerEventDTO
            {
                Type = ServerEventType.QuestProgressEvent,
                content = content.ToByteArray(),
            };
        }
    }
}
