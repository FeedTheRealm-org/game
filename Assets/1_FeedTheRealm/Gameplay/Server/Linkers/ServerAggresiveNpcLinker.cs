using FTR.Core.Server;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Server.Characters;
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

    public ServerAggresiveNpcLinker(
        WorldMonitor world,
        ServerPrefabProvider prefabProvider,
        IObjectResolver resolver
    )
    {
        this.characterLinker = new ServerCharacterLinker(world, prefabProvider, resolver);
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
    }

    public override void Link(GameObject gameObject)
    {
        gameObject.name = "AggressiveNPC";

        var netId = gameObject.GetComponent<NetworkIdentity>().netId;
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();

        var systems = characterLinker.Link(gameObject, netId);

        var enemyComponents = resolver.Instantiate(
            prefabProvider.ServerEnemyComponents,
            gameObject.transform
        );
        var enemyCommandHandler = enemyComponents.GetComponent<EnemyCommandHandler>();
        enemyCommandHandler.Initialize(systems.Movement, systems.Dash, systems.Use);

        characterLinker.RegisterEntity(netId, networkAdapter, enemyCommandHandler);
    }
}
