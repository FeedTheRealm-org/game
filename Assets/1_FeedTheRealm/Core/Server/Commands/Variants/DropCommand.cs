namespace FTR.Core.Server.Commands;

public class DropCommand : BaseServerCommand
{
    private string itemId;

    public DropCommand(string itemId)
    {
        this.itemId = itemId;
    }

    public override void Apply(ICommandable commandable)
    {
        commandable.OnDrop();
    }
}
