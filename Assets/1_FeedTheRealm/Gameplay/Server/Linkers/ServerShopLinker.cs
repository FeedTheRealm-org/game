using FTR.Core.Server;
using FTR.Core.Server.Entities;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Server.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Linkers;

public class ServerShopLinker : ShopLinker
{
    private readonly WorldMonitor world;
    private readonly ServerPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;

    public ServerShopLinker(
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
        Debug.Log(
            "[ShopLinker] Linking shop with netId: "
                + gameObject.GetComponent<NetworkIdentity>().netId
        );

        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        var netId = gameObject.GetComponent<NetworkIdentity>().netId;

        var tracker = gameObject.AddComponent<ServerEntityCleanupTracker>();
        tracker.Initialize(world, netId);

        // Add server-side components
        var serverComponents = resolver.Instantiate(
            prefabProvider.ServerShopComponent,
            gameObject.transform
        );
        serverComponents.layer = gameObject.layer;

        resolver.InjectGameObject(serverComponents);

        var logger = resolver.Resolve<Logging.Logger>();
        var shopInteractSystem = serverComponents.GetComponent<ShopInteractSystem>();

        shopInteractSystem.Initialize(logger, world, netId, gameObject.name);

        RegisterEntity(netId, networkAdapter);
    }

    public void RegisterEntity(uint netID, NetworkAdapter networkAdapter)
    {
        var entity = new ServerEntity(netID, networkAdapter, null);
        world.Entities.Register(netID, entity);
    }
}
