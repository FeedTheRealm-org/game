using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands;

public class UnequipItemCommand : BaseServerCommand
{
    private readonly string itemId;
    private readonly MoveItemCommandContent content;

    public UnequipItemCommand(uint netId, string itemId, MoveItemCommandContent content)
        : base(netId)
    {
        this.itemId = itemId;
        this.content = content;
    }

    public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
    {
        commandable.OnUnequipItem(
            eventCollector,
            content.SourcePosition,
            content.TargetPosition,
            itemId
        );
    }
}
