using FTR.Core.Server;
using FTR.Core.Server.Entities;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Portal;
using FTR.Gameplay.Server.Environment.Portal;
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
        Debug.Log($"[ServerPortalLinker] Linking portal GameObject: {gameObject.name}", gameObject);
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        var netId = gameObject.GetComponent<NetworkIdentity>().netId;
        var portalStateStorage = gameObject.GetComponent<PortalStateStorage>();

        // Add server-side components
        var serverComponents = Object.Instantiate(
            prefabProvider.PortalComponent,
            gameObject.transform
        );
        serverComponents.layer = gameObject.layer;
        resolver.InjectGameObject(serverComponents);

        serverComponents.GetComponent<PortalInteractSystem>().Initialize(world, portalStateStorage);
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
