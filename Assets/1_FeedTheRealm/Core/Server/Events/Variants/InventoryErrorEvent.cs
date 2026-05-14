using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;

namespace FTR.Core.Server.Events;

public class InventoryErrorEvent : BaseServerEvent
{
    private readonly InventoryErrorContent content;

    public InventoryErrorEvent(
        uint netId,
        InventoryErrorContent content,
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
            Type = ServerEventType.InventoryErrorEvent,
            content = new InventoryErrorContent { ErrorType = content.ErrorType }.ToByteArray(),
        };
    }
}
