using FTR.Core.Common.Protocol.RpcMessages;

namespace FTR.Core.Server.Events;

/// <summary>
/// Represents a server event that should be serialized & sent to
/// the given netId::NetworkAdapter.
/// If isTargeted is false, the event will be sent to all clients, otherwise just the owner.
/// </summary>
public abstract class BaseServerEvent
{
    public uint NetId { get; }

    public bool IsTargeted { get; }

    public BaseServerEvent(uint netId, bool isTargeted)
    {
        this.NetId = netId;
        this.IsTargeted = isTargeted;
    }

    public abstract ServerEventDTO ToDTO();
}
