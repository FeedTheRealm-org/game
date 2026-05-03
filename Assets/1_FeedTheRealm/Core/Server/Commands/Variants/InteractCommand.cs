using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands;

public class InteractCommand : BaseServerCommand
{
    public InteractCommand(uint netId)
        : base(netId) { }

    public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
    {
        commandable.OnInteract(eventCollector);
    }
}
