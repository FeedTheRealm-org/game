using Game.Core.Server.States;

namespace Game.Core.Server.Entities;

public sealed class ServerEntity
{
    public uint NetId { get; }

    public EntityState State;

    public NetworkAdapter NetworkAdapter { get; }
    public MovementController Movement { get; }

    public ServerEntity(uint netId, NetworkAdapter networkAdapter, MovementController movement)
    {
        NetId = netId;
        NetworkAdapter = networkAdapter;
        Movement = movement;
    }
}
