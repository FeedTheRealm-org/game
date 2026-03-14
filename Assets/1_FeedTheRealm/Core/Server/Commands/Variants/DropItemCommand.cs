using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands;

public class DropItemCommand : BaseServerCommand
{
    public string Id { get; }
    private DropItemCommandContent Content { get; }

    public DropItemCommand(uint netId, string id, DropItemCommandContent Content)
        : base(netId)
    {
        this.Id = id;
        this.Content = Content;
    }

    public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
    {
        commandable.OnDropItem(eventCollector, Content.Position);
    }
}
