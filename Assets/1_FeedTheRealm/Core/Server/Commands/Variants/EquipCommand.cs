namespace FTR.Core.Server.Commands;

public class EquipCommand : BaseServerCommand
{
    private string itemId;

    public EquipCommand(string itemId)
    {
        this.itemId = itemId;
    }

    public override void Apply(ICommandable commandable)
    {
        commandable.OnEquip();
    }
}
