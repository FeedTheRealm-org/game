using FTR.Core.Server.Events;
using UnityEngine;

namespace FTR.Core.Server.Commands;

public class DashCommand : BaseServerCommand
{
    private Vector3 direction;

    public DashCommand(uint netId, Vector3 direction)
        : base(netId)
    {
        this.direction = direction;
    }

    public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
    {
        commandable.OnDash();
    }
}
