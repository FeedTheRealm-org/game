using FTR.Core.Common.Protocol.RpcMessages;

namespace FTR.Core.Server.Events;

/// <summary>
/// Represents a server event that should be serialized & sent to
/// the given netId::NetworkAdapter.
/// If TargetConnectionId is null, the event will be sent to all clients (Broadcast).
/// If specified, it will only be sent to the client with that connection ID.
/// </summary>
public abstract class BaseServerEvent
{
    public uint NetId { get; }

    public int? TargetConnectionId { get; }

    public BaseServerEvent(uint netId, int? targetConnectionId = null)
    {
        this.NetId = netId;
        this.TargetConnectionId = targetConnectionId;
    }

    public abstract ServerEventDTO ToDTO();
}
