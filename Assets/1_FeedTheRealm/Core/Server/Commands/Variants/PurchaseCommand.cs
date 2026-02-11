namespace FTR.Core.Server.Commands;

public class PurchaseCommand : BaseServerCommand
{
    private string itemId;

    public PurchaseCommand(uint netId, string itemId)
        : base(netId)
    {
        this.itemId = itemId;
    }

    public override void Apply(ICommandable commandable)
    {
        commandable.OnPurchase();
    }
}
