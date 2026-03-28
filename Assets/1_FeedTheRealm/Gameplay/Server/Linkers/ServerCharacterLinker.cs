using FTR.Core.Server;
using FTR.Core.Server.Entities;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Server.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Linkers;

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

    public GameObject Link(GameObject gameObject, uint netId)
    {
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
        var rb = gameObject.GetComponent<Rigidbody>();
        var col = gameObject.GetComponent<Collider>();

        var serverComponents = resolver.Instantiate(
            prefabProvider.ServerCharacterComponents,
            gameObject.transform
        );

        var serverCommandHandler = serverComponents.GetComponent<ServerCommandHandler>();
        var movementSystem = serverComponents.GetComponent<MovementSystem>();
        var dashSystem = serverComponents.GetComponent<DashSystem>();
        var useSystem = serverComponents.GetComponent<UseSystem>();
        var groundCheckSystem = serverComponents.GetComponent<GroundCheckSystem>();
        var interactSystem = serverComponents.GetComponent<InteractSystem>();

        movementSystem.Initialize(rb, stateStorage);
        dashSystem.Initialize(netId, rb, stateStorage);
        groundCheckSystem.Initialize(col, stateStorage);
        interactSystem.Initialize(netId, stateStorage);

        serverCommandHandler.Initialize(movementSystem, dashSystem, useSystem, interactSystem);

        return serverComponents;
    }

    public void RegisterEntity(
        uint netId,
        NetworkAdapter networkAdapter,
        ServerCommandHandler commandHandler
    )
    {
        var entity = new ServerEntity(netId, networkAdapter, commandHandler);
        world.Entities.Register(netId, entity);
    }
}
