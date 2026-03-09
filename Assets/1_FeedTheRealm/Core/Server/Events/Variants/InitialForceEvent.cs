using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;

namespace FTR.Core.Server.Events;

public class InitialForceEvent : BaseServerEvent
{
    private InitialForceEventContent content;

    public InitialForceEvent(uint netId, InitialForceEventContent content)
        : base(netId, false)
    {
        this.content = content;
    }

    public override ServerEventDTO ToDTO()
    {
        return new ServerEventDTO
        {
            Type = Common.Enums.ServerEventType.InitialForceEvent,
            content = content.ToByteArray(),
        };
    }
}
