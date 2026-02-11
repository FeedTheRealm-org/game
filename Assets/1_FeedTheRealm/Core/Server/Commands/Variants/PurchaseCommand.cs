namespace FTR.Core.Server.Commands;

public class PurchaseCommand : BaseServerCommand
{
    private string itemId;

    public PurchaseCommand(string itemId)
    {
        this.itemId = itemId;
    }

    public override void Apply(ICommandable commandable)
    {
        commandable.OnPurchase();
    }
}
