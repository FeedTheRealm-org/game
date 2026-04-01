using FTR.Core.Server;
using FTR.Core.Server.Config;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Characters;
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
    private readonly WorldMonitor world;
    private readonly ServerConfig config;

    public ServerPlayerLinker(
        WorldMonitor world,
        ServerPrefabProvider prefabProvider,
        IObjectResolver resolver,
        ServerConfig config
    )
    {
        this.world = world;
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

        int connectionId = networkAdapter.connectionToClient.connectionId;

        var tracker = gameObject.AddComponent<ServerEntityCleanupTracker>();
        tracker.Initialize(world, netId);

        var systems = characterLinker.Link(gameObject, netId);
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();

        var playerComponents = resolver.Instantiate(
            prefabProvider.ServerPlayerComponents,
            gameObject.transform
        );

        var serverPlayerCommandHandler =
            playerComponents.GetComponent<ServerPlayerCommandHandler>();
        var respawnSystem = playerComponents.GetComponent<RespawnSystem>();
        var persistenceSystem = playerComponents.GetComponent<PersistenceSystem>();
        var inventorySystem = playerComponents.GetComponent<InventorySystem>();
        var interactSystem = playerComponents.GetComponent<PlayerInteractSystem>();
        var questSystem = playerComponents.GetComponent<QuestSystem>();

        interactSystem.Initialize(netId, world, networkAdapter.netId);
        questSystem.Initialize(netId);
        systems.Health.Initialize(netId, stateStorage, false);
        systems.Use.Initialize(netId, rb, config.PlayerLayer | config.TargetLayer, stateStorage);
        inventorySystem.Initialize(netId, inventoryStateStorage);
        persistenceSystem.Initialize(systems.Movement, inventorySystem);

        serverPlayerCommandHandler.Initialize(
            systems.Movement,
            systems.Dash,
            systems.Use,
            interactSystem,
            inventorySystem,
            questSystem
        );

        respawnSystem.Initialize(
            netId,
            networkAdapter,
            serverPlayerCommandHandler,
            rb,
            systems.Health
        );

        characterLinker.RegisterEntity(
            netId,
            networkAdapter,
            serverPlayerCommandHandler,
            connectionId
        );
    }
}
