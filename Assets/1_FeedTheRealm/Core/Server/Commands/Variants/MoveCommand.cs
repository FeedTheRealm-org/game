using UnityEngine;

namespace FTR.Core.Server.Commands;

public class MoveCommand : BaseServerCommand
{
    private Vector3 direction;

    public MoveCommand(Vector3 direction)
    {
        this.direction = direction;
    }

    public override void Apply(ICommandable commandable)
    {
        commandable.OnMove();
    }
}
