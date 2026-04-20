using API;
using FTR.Core.Common.Config;
using FTR.Core.Server;
using FTR.Core.Server.Config;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.NetworkEntities.Gold;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using FTR.Gameplay.Server.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using FTR.Gameplay.Server.Registry;
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
    private readonly ServerConfig serverConfig;
    private readonly Config commonConfig;
    private readonly PlayerService playerService;
    private readonly PortalRegistry portalRegistry;

    public ServerPlayerLinker(
        WorldMonitor world,
        ServerPrefabProvider prefabProvider,
        IObjectResolver resolver,
        ServerConfig serverConfig,
        Config commonConfig,
        PlayerService playerService,
        PortalRegistry portalRegistry
    )
    {
        this.world = world;
        this.characterLinker = new ServerCharacterLinker(world, prefabProvider, resolver);
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
        this.serverConfig = serverConfig;
        this.commonConfig = commonConfig;
        this.playerService = playerService;
        this.portalRegistry = portalRegistry;
    }

    public override void Link(GameObject gameObject)
    {
        gameObject.name = "Player";

        var netId = gameObject.GetComponent<NetworkIdentity>().netId;
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        var inventoryStateStorage = gameObject.GetComponent<InventoryStateStorage>();
        var goldStateStorage = gameObject.GetComponent<GoldStateStorage>();
        var rb = gameObject.GetComponent<Rigidbody>();

        int connectionId = networkAdapter.connectionToClient.connectionId;

        var tracker = gameObject.AddComponent<ServerEntityCleanupTracker>();
        tracker.Initialize(world, netId);

        var sharedSystems = characterLinker.Link(gameObject, netId);
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();

        var playerComponents = resolver.Instantiate(
            prefabProvider.ServerPlayerComponents,
            gameObject.transform
        );

        var serverPlayerCommandHandler =
            playerComponents.GetComponent<ServerPlayerCommandHandler>();
        var respawnSystem = playerComponents.GetComponent<RespawnSystem>();
        var playerPersistenceSystem = playerComponents.GetComponent<PlayerPersistenceSystem>();
        var inventorySystem = playerComponents.GetComponent<InventorySystem>();
        var goldSystem = playerComponents.GetComponent<GoldSystem>();
        var interactSystem = playerComponents.GetComponent<PlayerInteractSystem>();
        var questSystem = playerComponents.GetComponent<QuestSystem>();
        var teleportSystem = playerComponents.GetComponent<TeleportSystem>();
        var chatSystem = playerComponents.GetComponent<ChatSystem>();

        interactSystem.Initialize(netId, world, networkAdapter.netId);
        questSystem.Initialize(netId, world, networkAdapter.netId);
        sharedSystems.Health.Initialize(netId, stateStorage, false);
        sharedSystems.Use.Initialize(
            netId,
            rb,
            serverConfig.PlayerLayer | serverConfig.TargetLayer,
            stateStorage
        );
        inventorySystem.Initialize(netId, inventoryStateStorage, stateStorage);
        goldSystem.Initialize(netId, goldStateStorage, world);
        playerPersistenceSystem.Initialize(
            stateStorage,
            inventorySystem,
            goldSystem,
            sharedSystems.Movement,
            questSystem
        );
        chatSystem.Initialize(netId, world);

        teleportSystem.Initialize(sharedSystems.Movement, portalRegistry, world, netId);

        serverPlayerCommandHandler.Initialize(
            sharedSystems.Movement,
            sharedSystems.Dash,
            sharedSystems.Use,
            interactSystem,
            inventorySystem,
            questSystem,
            stateStorage,
            playerService,
            commonConfig.ServerAccessToken,
            goldSystem,
            teleportSystem,
            chatSystem
        );

        respawnSystem.Initialize(
            netId,
            networkAdapter,
            serverPlayerCommandHandler,
            rb,
            sharedSystems.Health
        );

        characterLinker.RegisterEntity(
            netId,
            networkAdapter,
            serverPlayerCommandHandler,
            connectionId
        );
    }
}
