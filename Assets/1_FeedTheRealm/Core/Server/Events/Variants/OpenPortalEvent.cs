using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;

namespace FTR.Core.Server.Events;

public class OpenPortalEvent : BaseServerEvent
{
    private readonly OpenPortalEventContent content;

    public OpenPortalEvent(
        uint netId,
        OpenPortalEventContent content,
        int? targetConnectionId = null
    )
        : base(netId, targetConnectionId)
    {
        this.content = content;
    }

    public override ServerEventDTO ToDTO()
    {
        return new ServerEventDTO
        {
            Type = ServerEventType.OpenPortalEvent,
            content = new OpenPortalEventContent
            {
                PortalId = content.PortalId,
                PortalName = content.PortalName,
                DestinationName = content.DestinationName,
                DestinationZone = content.DestinationZone,
            }.ToByteArray(),
        };
    }
}
