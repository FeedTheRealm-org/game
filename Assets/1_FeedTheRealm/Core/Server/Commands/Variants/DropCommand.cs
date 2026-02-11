using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands;

public class DropCommand : BaseServerCommand
{
    private string itemId;

    public DropCommand(uint netId, string itemId)
        : base(netId)
    {
        this.itemId = itemId;
    }

    public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
    {
        commandable.OnDrop();
    }
}
