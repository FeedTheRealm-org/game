using FTR.Core.Common.Utils;
using FTR.Core.Server.Commands;
using FTR.Core.Server.States;

namespace FTR.Core.Server.Entities;

public sealed class ServerEntity
{
    public uint NetId { get; }

    public bool IsPlayer { get; private set; }

    public NetworkAdapter NetworkAdapter { get; }
    public ICommandable Commandable { get; }
    public int? ConnectionId { get; }

    /// <summary>
    /// Creates a new ServerEntity with the given netId, networkAdapter, and entity commandable.
    /// </summary>
    public ServerEntity(
        uint netId,
        NetworkAdapter networkAdapter,
        ICommandable commandable,
        int? connectionId = null,
        bool isPlayer = false
    )
    {
        NetId = netId;
        NetworkAdapter = networkAdapter;
        this.Commandable = commandable;
        this.ConnectionId = connectionId;
        this.IsPlayer = isPlayer;
    }
}
