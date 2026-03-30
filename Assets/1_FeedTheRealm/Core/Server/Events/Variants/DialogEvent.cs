using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;

namespace FTR.Core.Server.Events;

public class DialogEvent : BaseServerEvent
{
    private readonly DialogEventContent content;

    public DialogEvent(uint netId, DialogEventContent content, int? targetConnectionId = null)
        : base(netId, targetConnectionId)
    {
        this.content = content;
    }

    public override ServerEventDTO ToDTO()
    {
        return new ServerEventDTO
        {
            Type = ServerEventType.DialogEvent,
            content = content.ToByteArray(),
        };
    }
}
