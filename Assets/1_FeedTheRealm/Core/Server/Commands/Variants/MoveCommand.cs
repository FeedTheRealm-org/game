using FTR.Core.Server.Events;
using UnityEngine;

namespace FTR.Core.Server.Commands;

public class MoveCommand : BaseServerCommand
{
    private Vector3 direction;

    public MoveCommand(uint netId, Vector3 direction)
        : base(netId)
    {
        this.direction = direction;
    }

    public override void Apply(ICommandable commandable, IEventCollectable eventCollector)
    {
        commandable.OnMove();
    }
}
