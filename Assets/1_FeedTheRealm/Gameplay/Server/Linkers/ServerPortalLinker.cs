using FTR.Core.Server;
using FTR.Core.Server.Entities;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Server.Environment.Portal;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Linkers;

public class ServerPortalLinker : LootItemLinker
{
    private readonly WorldMonitor world;
    private readonly ServerPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;

    public ServerPortalLinker(
        WorldMonitor world,
        ServerPrefabProvider prefabProvider,
        IObjectResolver resolver
    )
    {
        this.world = world;
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
    }

    public override void Link(GameObject gameObject)
    {
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        var netId = gameObject.GetComponent<NetworkIdentity>().netId;

        // Add server-side components
        var serverComponents = Object.Instantiate(
            prefabProvider.PortalComponent,
            gameObject.transform
        );
        serverComponents.layer = gameObject.layer;
        resolver.InjectGameObject(serverComponents);

        RegisterEntity(netId, networkAdapter);
    }

    public void RegisterEntity(uint netID, NetworkAdapter networkAdapter)
    {
        var entity = new ServerEntity(netID, networkAdapter, null);
        world.Entities.Register(netID, entity);
    }
}
