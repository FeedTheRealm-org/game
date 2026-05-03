using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands;

public class MoveItemCommand : BaseServerCommand
{
    public string Id { get; }
    private MoveItemCommandContent Content { get; }

    public MoveItemCommand(uint netId, string id, MoveItemCommandContent content)
        : base(netId)
    {
        Id = id;
        Content = content;
    }

    public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
    {
        commandable.OnMoveItem(
            eventCollector,
            Content.SourceType,
            Content.SourcePosition,
            Content.TargetType,
            Content.TargetPosition
        );
    }
}
