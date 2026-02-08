using Game.Core.Server.States;

namespace Game.Core.Server.Entities;

public sealed class CharacterComponents
{
    // MovementComponent
    // DashComponent
    // HealthComponent
    // StaminaComponent
    // UseComponent
}

public sealed class ServerEntity
{
    public uint NetId { get; }

    public EntityState State;

    public NetworkAdapter NetworkAdapter { get; }
    public CharacterComponents CharacterComponents { get; }

    /// <summary>
    /// Creates a new ServerEntity with the given netId and networkAdapter, and no characterComponents.
    /// </summary>
    public ServerEntity(uint netId, NetworkAdapter networkAdapter)
    {
        NetId = netId;
        NetworkAdapter = networkAdapter;
        CharacterComponents = null;
    }

    /// <summary>
    /// Creates a new ServerEntity with the given netId, networkAdapter, and characterComponents.
    /// </summary>
    public ServerEntity(
        uint netId,
        NetworkAdapter networkAdapter,
        CharacterComponents characterComponents
    )
    {
        NetId = netId;
        NetworkAdapter = networkAdapter;
        CharacterComponents = characterComponents;
    }
}
