using FTR.Core.Server;
using FTR.Core.Server.Config;
using FTR.Core.Server.Entities;
using FTR.Gameplay.Common.Environment.Structures;
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
    private readonly ServerConfig config;

    public ServerShopLinker(
        WorldMonitor world,
        ServerPrefabProvider prefabProvider,
        ServerConfig config,
        IObjectResolver resolver
    )
    {
        this.world = world;
        this.prefabProvider = prefabProvider;
        this.config = config;
        this.resolver = resolver;
    }

    public override void Link(GameObject gameObject)
    {
        Debug.Log(
            "[ShopLinker] Linking shop with netId: "
                + gameObject.GetComponent<NetworkIdentity>().netId
        );

        var structureController = gameObject.GetComponent<StructureController>();

        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        var netId = gameObject.GetComponent<NetworkIdentity>().netId;

        var tracker = gameObject.AddComponent<ServerEntityCleanupTracker>();
        tracker.Initialize(world, netId);

        // Add server-side components
        var serverComponents = resolver.Instantiate(
            prefabProvider.ServerShopComponent,
            gameObject.transform
        );
        serverComponents.layer = config.InteractableLayer;

        var boxCollider = serverComponents.AddComponent<BoxCollider>();
        boxCollider.size = structureController.Data.colliderSize;
        boxCollider.center = structureController.Data.colliderCenter;

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
