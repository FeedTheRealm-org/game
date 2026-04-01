using FTR.Core.Server;
using FTR.Core.Server.Entities;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using FTR.Gameplay.Server.Characters.Systems;
using FTR.Gameplay.Server.Environment.LootItem;
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
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        var netId = gameObject.GetComponent<NetworkIdentity>().netId;

        // Add server-side components
        var serverComponents = Object.Instantiate(
            prefabProvider.ServerLootItemComponents,
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
