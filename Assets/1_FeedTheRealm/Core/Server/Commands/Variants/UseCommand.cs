using UnityEngine;

namespace FTR.Core.Server.Commands;

public class UseCommand : BaseServerCommand
{
    private Vector3 direction;

    public UseCommand(Vector3 direction)
    {
        this.direction = direction;
    }

    public override void Apply(ICommandable commandable)
    {
        commandable.OnUse();
    }
}
