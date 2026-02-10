namespace FTR.Core.Server.Commands;

public abstract class BaseServerCommand
{
    public abstract void Apply(ICommandable commandable);
}
