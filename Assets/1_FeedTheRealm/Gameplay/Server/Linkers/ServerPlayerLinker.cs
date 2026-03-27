using FTR.Core.Server;
using FTR.Core.Server.Config;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using FTR.Gameplay.Server.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Linkers;

public class ServerPlayerLinker : PlayerLinker
{
    private readonly ServerCharacterLinker characterLinker;
    private readonly ServerPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;
    private readonly ServerConfig config;

    public ServerPlayerLinker(
        WorldMonitor world,
        ServerPrefabProvider prefabProvider,
        IObjectResolver resolver,
        ServerConfig config
    )
    {
        this.characterLinker = new ServerCharacterLinker(world, prefabProvider, resolver);
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
        this.config = config;
    }

    public override void Link(GameObject gameObject)
    {
        gameObject.name = "Player";

        var netId = gameObject.GetComponent<NetworkIdentity>().netId;
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        var inventoryStateStorage = gameObject.GetComponent<InventoryStateStorage>();
        var rb = gameObject.GetComponent<Rigidbody>();

        var serverComponents = characterLinker.Link(gameObject, netId);
        var movementSystem = serverComponents.GetComponent<MovementSystem>();
        var dashSystem = serverComponents.GetComponent<DashSystem>();
        var useSystem = serverComponents.GetComponent<UseSystem>();
        var interactSystem = serverComponents.GetComponent<InteractSystem>();
        var healthSystem = serverComponents.GetComponent<HealthSystem>();
        var serverCommandHandler = serverComponents.GetComponent<ServerCommandHandler>();

        var playerComponents = resolver.Instantiate(
            prefabProvider.ServerPlayerComponents,
            gameObject.transform
        );
        var serverPlayerCommandHandler =
            playerComponents.GetComponent<ServerPlayerCommandHandler>();
        var respawnSystem = playerComponents.GetComponent<RespawnSystem>();
        var persistenceSystem = playerComponents.GetComponent<PersistenceSystem>();
        var inventorySystem = playerComponents.GetComponent<InventorySystem>();

        useSystem.Initialize(netId, rb, config.PlayerLayer | config.TargetLayer);
        inventorySystem.Initialize(netId, inventoryStateStorage);
        respawnSystem.Initialize(netId, networkAdapter, serverCommandHandler, rb, healthSystem);
        persistenceSystem.Initialize(movementSystem, inventorySystem);
        serverPlayerCommandHandler.Initialize(
            movementSystem,
            dashSystem,
            useSystem,
            interactSystem,
            inventorySystem
        );

        characterLinker.RegisterEntity(netId, networkAdapter, serverPlayerCommandHandler);
    }
}
