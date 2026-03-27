using FTR.Core.Server;
using FTR.Core.Server.Config;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.Utils;
using FTR.Gameplay.Server.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using Mirror;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Linkers;

public class ServerAggresiveNpcLinker : AggresiveNpcLinker
{
    private readonly WorldMonitor world;
    private ServerCharacterLinker characterLinker;
    private readonly ServerPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;
    private readonly ServerConfig config;

    public ServerAggresiveNpcLinker(
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
        gameObject.name = $"AgressiveNPC";

        var netId = gameObject.GetComponent<NetworkIdentity>().netId;
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        var rb = gameObject.GetComponent<Rigidbody>();

        var serverComponents = characterLinker.Link(gameObject, netId);

        var serverCommandHandler = serverComponents.GetComponent<ServerCommandHandler>();
        var movementSystem = serverComponents.GetComponent<MovementSystem>();
        var dashSystem = serverComponents.GetComponent<DashSystem>();
        var useSystem = serverComponents.GetComponent<UseSystem>();
        var interactSystem = serverComponents.GetComponent<InteractSystem>();
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
        var aiNavigationSystem = serverComponents.AddComponent<AINavigationSystem>();

        resolver.Inject(aiNavigationSystem);

        var chaseTriggerArea = UnityEngine
            .Object.Instantiate(prefabProvider.PlayerTriggerAreaPrefab, gameObject.transform)
            .GetComponent<PlayerTriggerArea>();
        var attackTriggerArea = UnityEngine
            .Object.Instantiate(prefabProvider.PlayerTriggerAreaPrefab, gameObject.transform)
            .GetComponent<PlayerTriggerArea>();

        useSystem.Initialize(netId, rb, config.PlayerLayer, stateStorage);
        chaseTriggerArea.Initialize(config.AggressiveChaseRadius);
        attackTriggerArea.Initialize(config.AggressiveAttackRadius);
        serverCommandHandler.Initialize(movementSystem, dashSystem, useSystem, interactSystem);
        aiNavigationSystem.Initialize(netId, world, stateStorage);

        aiNavigationSystem.SetChaseTriggerArea(chaseTriggerArea);
        useSystem.SetAttackTriggerArea(attackTriggerArea);

        characterLinker.RegisterEntity(netId, networkAdapter, serverCommandHandler);
    }
}
