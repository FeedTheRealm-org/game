using FTR.Core.Server.Commands;
using FTR.Core.Server.States;

namespace FTR.Core.Server.Entities;

public sealed class ServerEntity
{
    public uint NetId { get; }

    public EntityState State;

    public NetworkAdapter NetworkAdapter { get; }
    public ICommandable commandable { get; }

    /// <summary>
    /// Creates a new ServerEntity with the given netId, networkAdapter, and entity commandable.
    /// </summary>
    public ServerEntity(uint netId, NetworkAdapter networkAdapter, ICommandable commandable)
    {
        NetId = netId;
        NetworkAdapter = networkAdapter;
        this.commandable = commandable;
    }
}
