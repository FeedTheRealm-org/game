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
using VContainer.Unity;

namespace FTR.Gameplay.Server.Linkers;

public class ServerAggresiveNpcLinker : AggresiveNpcLinker
{
    private readonly ServerCharacterLinker characterLinker;
    private readonly ServerPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;
    private readonly WorldMonitor world;
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
        gameObject.name = "AggressiveNPC";

        var netId = gameObject.GetComponent<NetworkIdentity>().netId;
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();

        var systems = characterLinker.Link(gameObject, netId);
        var rb = gameObject.GetComponent<Rigidbody>();

        var enemyComponents = resolver.Instantiate(
            prefabProvider.ServerEnemyComponents,
            gameObject.transform
        );
        var enemyCommandHandler = enemyComponents.GetComponent<EnemyCommandHandler>();

        characterLinker.RegisterEntity(netId, networkAdapter, enemyCommandHandler);
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
        var aiNavigationSystem = enemyComponents.AddComponent<AINavigationSystem>();

        resolver.Inject(aiNavigationSystem);

        var chaseTriggerArea = Object
            .Instantiate(prefabProvider.PlayerTriggerAreaPrefab, gameObject.transform)
            .GetComponent<PlayerTriggerArea>();
        var attackTriggerArea = Object
            .Instantiate(prefabProvider.PlayerTriggerAreaPrefab, gameObject.transform)
            .GetComponent<PlayerTriggerArea>();

        systems.Health.Initialize(netId, stateStorage, false);
        systems.Use.Initialize(netId, rb, config.PlayerLayer, stateStorage);
        chaseTriggerArea.Initialize(config.AggressiveChaseRadius);
        attackTriggerArea.Initialize(config.AggressiveAttackRadius);

        enemyCommandHandler.Initialize(systems.Movement, systems.Dash, systems.Use);
        aiNavigationSystem.Initialize(netId, world, stateStorage);

        aiNavigationSystem.SetChaseTriggerArea(chaseTriggerArea);
        systems.Use.SetAttackTriggerArea(attackTriggerArea);
    }
}
