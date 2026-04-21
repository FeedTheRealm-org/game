using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands;

public class AcceptTeleportCommand : BaseServerCommand
{
    private string portalId;

    public AcceptTeleportCommand(uint netId, string portalId)
        : base(netId)
    {
        this.portalId = portalId;
    }

    public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
    {
        commandable.OnTeleportAccepted(eventCollector, portalId);
    }
}
