namespace FTR.Core.Server.Commands;

public class EquipCommand : BaseServerCommand
{
    private string itemId;

    public EquipCommand(uint netId, string itemId)
        : base(netId)
    {
        this.itemId = itemId;
    }

    public override void Apply(ICommandable commandable)
    {
        commandable.OnEquip();
    }
}
