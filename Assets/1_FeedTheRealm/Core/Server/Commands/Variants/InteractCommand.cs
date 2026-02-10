using UnityEngine;

namespace FTR.Core.Server.Commands;

public class InteractCommand : BaseServerCommand
{
    private Vector3 direction;

    public InteractCommand(Vector3 direction)
    {
        this.direction = direction;
    }

    public override void Apply(ICommandable commandable)
    {
        commandable.OnInteract();
    }
}
