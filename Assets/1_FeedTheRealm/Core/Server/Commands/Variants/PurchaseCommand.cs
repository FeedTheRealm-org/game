using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands;

public class PurchaseCommand : BaseServerCommand
{
    public string Id { get; }
    private PurchaseCommandContent Content { get; }

    public PurchaseCommand(uint netId, string id, PurchaseCommandContent content)
        : base(netId)
    {
        Id = id;
        Content = content;
    }

    public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
    {
        commandable.OnPurchase(
            eventCollector,
            NetId,
            Content.ProductId,
            Content.Amount,
            Content.ShopId
        );
    }
}
