using UnityEngine;

namespace FTR.Core.Server.Commands;

public class PurchaseCommand : BaseServerCommand
{
    private Vector3 direction;

    public PurchaseCommand(Vector3 direction)
    {
        this.direction = direction;
    }

    public override void Apply(ICommandable commandable)
    {
        commandable.OnPurchase();
    }
}
