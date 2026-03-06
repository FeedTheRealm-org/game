using FTR.Core.Common.Loaders;
using FTR.Core.Server;
using FTR.Core.Server.Entities;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Characters;

public class ServerCharacterLinker : IScriptLinker
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

    public void LinkDomainScripts(GameObject gameObject)
    {
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
        var rb = gameObject.GetComponent<Rigidbody>();
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();

        // Add server-side components
        var serverComponents = Object.Instantiate(
            prefabProvider.ServerPlayerComponents,
            gameObject.transform
        );

        resolver.InjectGameObject(serverComponents);

        var serverCommandHandler = serverComponents.GetComponent<ServerCommandHandler>();
        var movementSystem = serverComponents.GetComponent<MovementSystem>();
        var useSystem = serverComponents.GetComponent<UseSystem>();
        var healthSystem = serverComponents.GetComponent<HealthSystem>();

        // Initialize components
        movementSystem.Initialize(rb, stateStorage);
        useSystem.Initialize(rb);
        serverCommandHandler.Initialize(movementSystem, useSystem);

        var netID = gameObject.GetComponent<NetworkIdentity>().netId;
        RegisterEntity(netID, networkAdapter, serverCommandHandler);
        gameObject.name = $"Player-{netID}";

        Debug.Log($"Linked domain scripts for character with netID {netID}");
    }

    public void RegisterEntity(
        uint netID,
        NetworkAdapter networkAdapter,
        ServerCommandHandler serverCommandHandler
    )
    {
        var entity = new ServerEntity(netID, networkAdapter, serverCommandHandler);

        world.Entities.Register(netID, entity);
    }
}
