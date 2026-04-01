using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;

namespace FTR.Core.Server.Events;

public class OpenShopEvent : BaseServerEvent
{
    private readonly OpenShopEventContent content;

    public OpenShopEvent(uint netId, OpenShopEventContent content, int? targetConnectionId = null)
        : base(netId, targetConnectionId)
    {
        this.content = content;
    }

    public override ServerEventDTO ToDTO()
    {
        return new ServerEventDTO
        {
            Type = ServerEventType.OpenShopEvent,
            content = new OpenShopEventContent { ShopId = content.ShopId }.ToByteArray(),
        };
    }
}
