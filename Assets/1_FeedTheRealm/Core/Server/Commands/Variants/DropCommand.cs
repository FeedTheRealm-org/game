using UnityEngine;

namespace FTR.Core.Server.Commands;

public class DropCommand : BaseServerCommand
{
    private Vector3 direction;

    public DropCommand(Vector3 direction)
    {
        this.direction = direction;
    }

    public override void Apply(ICommandable commandable)
    {
        commandable.OnDrop();
    }
}
