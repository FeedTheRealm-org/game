using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using Google.Protobuf;

namespace FTR.Core.Server.Events;

public class NotEnoughGoldEvent : BaseServerEvent
{
    private readonly NotEnoughGoldEventContent content;

    public NotEnoughGoldEvent(
        uint netId,
        NotEnoughGoldEventContent content,
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
            Type = ServerEventType.NotEnoughGoldEvent,
            content = new NotEnoughGoldEventContent
            {
                ProductId = content.ProductId,
                Amount = content.Amount,
            }.ToByteArray(),
        };
    }
}
