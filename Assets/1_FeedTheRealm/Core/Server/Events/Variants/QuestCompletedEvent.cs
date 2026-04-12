using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;

namespace FTR.Core.Server.Events
{
    public class QuestCompletedEvent : BaseServerEvent
    {
        private readonly QuestCompletedEventContent content;

        public QuestCompletedEvent(
            uint netId,
            int targetConnectionId,
            QuestCompletedEventContent content
        )
            : base(netId, targetConnectionId)
        {
            this.content = content;
        }

        public override ServerEventDTO ToDTO()
        {
            return new ServerEventDTO
            {
                Type = ServerEventType.QuestCompletedEvent,
                content = content.ToByteArray(),
            };
        }
    }
}
