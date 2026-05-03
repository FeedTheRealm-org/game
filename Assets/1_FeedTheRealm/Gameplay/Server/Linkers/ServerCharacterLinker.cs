using FTR.Core.Server;
using FTR.Core.Server.Commands;
using FTR.Core.Server.Entities;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Linkers;

/// <summary>
/// Instantiates and initializes the shared ServerCharacterComponents prefab.
/// Each entity linker is responsible for instantiating its own CommandHandler prefab
/// and registering the entity with the appropriate handler.
/// </summary>
public class ServerCharacterLinker
{
    private readonly WorldMonitor world;
    private readonly ServerPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;

    public ServerCharacterLinker(
        WorldMonitor world,
        ServerPrefabProvider prefabProvider,
        IObjectResolver resolver
    )
    {
        this.world = world;
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
    }

    /// <summary>
    /// Instantiates ServerCharacterComponents and initializes all shared systems.
    /// Does NOT initialize or return any CommandHandler — each linker handles that separately.
    /// </summary>
    public ServerCharacterSystems Link(GameObject gameObject, uint netId)
    {
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
        var rb = gameObject.GetComponent<Rigidbody>();
        var col = gameObject.GetComponent<Collider>();

        var serverComponents = resolver.Instantiate(
            prefabProvider.ServerCharacterComponents,
            gameObject.transform
        );

        var movementSystem = serverComponents.GetComponent<MovementSystem>();
        var healthSystem = serverComponents.GetComponent<HealthSystem>();
        var dashSystem = serverComponents.GetComponent<DashSystem>();
        var useSystem = serverComponents.GetComponent<UseSystem>();
        var groundCheckSystem = serverComponents.GetComponent<GroundCheckSystem>();

        movementSystem.Initialize(netId, rb, stateStorage);
        dashSystem.Initialize(netId, rb, stateStorage);
        groundCheckSystem.Initialize(col, stateStorage);

        return new ServerCharacterSystems(
            serverComponents,
            movementSystem,
            dashSystem,
            useSystem,
            healthSystem,
            groundCheckSystem
        );
    }

    public void RegisterEntity(
        uint netId,
        NetworkAdapter networkAdapter,
        ICommandable commandHandler,
        int? connectionId = null,
        bool isPlayer = false
    )
    {
        var entity = new ServerEntity(
            netId,
            networkAdapter,
            commandHandler,
            connectionId,
            isPlayer
        );
        world.Entities.Register(netId, entity);
    }
}

public sealed class ServerCharacterSystems
{
    public GameObject GameObject { get; }
    public MovementSystem Movement { get; }
    public DashSystem Dash { get; }
    public UseSystem Use { get; }
    public HealthSystem Health { get; }
    public GroundCheckSystem GroundCheck { get; }

    public ServerCharacterSystems(
        GameObject gameObject,
        MovementSystem movement,
        DashSystem dash,
        UseSystem use,
        HealthSystem health,
        GroundCheckSystem groundCheck
    )
    {
        GameObject = gameObject;
        Movement = movement;
        Dash = dash;
        Use = use;
        Health = health;
        GroundCheck = groundCheck;
    }
}
