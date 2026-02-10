using UnityEngine;

namespace FTR.Core.Server.Commands;

public class EquipCommand : BaseServerCommand
{
    private Vector3 direction;

    public EquipCommand(Vector3 direction)
    {
        this.direction = direction;
    }

    public override void Apply(ICommandable commandable)
    {
        commandable.OnEquip();
    }
}
