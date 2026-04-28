using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;

namespace FTR.Core.Server.Events
{
    public class ChatMessageBroadcastEvent : BaseServerEvent
    {
        private readonly ChatMessageBroadcastEventContent content;

        public ChatMessageBroadcastEvent(uint netId, ChatMessageBroadcastEventContent content)
            : base(netId)
        {
            this.content = content;
        }

        public override ServerEventDTO ToDTO()
        {
            return new ServerEventDTO
            {
                Type = ServerEventType.ChatMessageBroadcastEvent,
                content = content.ToByteArray(),
            };
        }
    }
}
