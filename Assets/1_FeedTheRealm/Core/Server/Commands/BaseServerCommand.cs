using FTR.Core.Server.Events;

namespace FTR.Core.Server.Commands;

/// <summary>
/// Represents a server command that should be processed by the GameLoop
/// and invoked on the desired entity with the given netId.
/// </summary>
public abstract class BaseServerCommand
{
    public uint NetId { get; }

    public BaseServerCommand(uint netId)
    {
        NetId = netId;
    }

    public abstract void Apply(ICommandable commandable, IEventCollectable eventCollector);
}
