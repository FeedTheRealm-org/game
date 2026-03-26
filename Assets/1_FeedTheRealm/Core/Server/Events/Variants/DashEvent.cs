using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;

namespace FTR.Core.Server.Events;

public class DashEvent : BaseServerEvent
{
    private DashEventContent content;

    public DashEvent(uint netId, DashEventContent content)
        : base(netId)
    {
        this.content = content;
    }

    public override ServerEventDTO ToDTO()
    {
        return new ServerEventDTO
        {
            Type = Common.Enums.ServerEventType.DashEvent,
            content = content.ToByteArray(),
        };
    }
}
