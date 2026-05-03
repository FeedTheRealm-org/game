using FTR.Core.Server;
using FTR.Core.Server.Entities;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Portal;
using FTR.Gameplay.Server.Characters;
using FTR.Gameplay.Server.Environment.Portal;
using FTR.Gameplay.Server.Registry;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Linkers;

public class ServerPortalLinker : PortalLinker
{
    private readonly WorldMonitor world;
    private readonly ServerPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;
    private readonly PortalRegistry portalRegistry;

    public ServerPortalLinker(
        WorldMonitor world,
        ServerPrefabProvider prefabProvider,
        PortalRegistry portalRegistry,
        IObjectResolver resolver
    )
    {
        this.world = world;
        this.prefabProvider = prefabProvider;
        this.portalRegistry = portalRegistry;
        this.resolver = resolver;
    }

    public override void Link(GameObject gameObject)
    {
        Debug.Log($"[ServerPortalLinker] Linking portal GameObject: {gameObject.name}", gameObject);
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        var netId = gameObject.GetComponent<NetworkIdentity>().netId;
        var portalStateStorage = gameObject.GetComponent<PortalStateStorage>();

        var tracker = gameObject.AddComponent<ServerEntityCleanupTracker>();
        tracker.Initialize(world, netId);

        // Add server-side components
        var serverComponents = Object.Instantiate(
            prefabProvider.PortalComponent,
            gameObject.transform
        );
        serverComponents.layer = gameObject.layer;
        resolver.InjectGameObject(serverComponents);

        serverComponents
            .GetComponent<PortalInteractSystem>()
            .Initialize(world, portalStateStorage, portalRegistry);
        serverComponents.GetComponent<PortalController>().Initialize(netId);

        RegisterEntity(netId, networkAdapter);

        Debug.Log(
            $"[ServerPortalLinker] Linked portal with NetID {netId} and registered in world. server components: {serverComponents != null}"
        );
    }

    public void RegisterEntity(uint netID, NetworkAdapter networkAdapter)
    {
        var entity = new ServerEntity(netID, networkAdapter, null);
        world.Entities.Register(netID, entity);
    }
}
