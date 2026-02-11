namespace FTR.Core.Server.Commands;

public abstract class BaseServerCommand
{
    public uint NetId { get; }

    public BaseServerCommand(uint netId)
    {
        NetId = netId;
    }

    public abstract void Apply(ICommandable commandable);
}
