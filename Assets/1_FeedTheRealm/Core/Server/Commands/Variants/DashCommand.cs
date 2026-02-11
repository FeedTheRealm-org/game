using UnityEngine;

namespace FTR.Core.Server.Commands;

public class DashCommand : BaseServerCommand
{
    private Vector3 direction;

    public DashCommand(Vector3 direction)
    {
        this.direction = direction;
    }

    public override void Apply(ICommandable commandable)
    {
        commandable.OnDash();
    }
}
