using UnityEngine;

namespace FTR.Core.Server.Commands;

public class InteractCommand : BaseServerCommand
{
    private Vector3 direction;

    public InteractCommand(uint netId, Vector3 direction)
        : base(netId)
    {
        this.direction = direction;
    }

    public override void Apply(ICommandable commandable)
    {
        commandable.OnInteract();
    }
}
