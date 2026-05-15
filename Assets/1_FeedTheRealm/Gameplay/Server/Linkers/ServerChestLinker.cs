using FTR.Core.Server;
using FTR.Core.Server.Entities;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Chest;
using FTR.Gameplay.Server.Environment.Chest;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Linkers;

public class ServerChestLinker : ChestLinker
{
    private readonly WorldMonitor world;
    private readonly ServerPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;

    public ServerChestLinker(
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
        var chestStateStorage = gameObject.GetComponent<ChestStateStorage>();

        // Add server-side components
        var serverComponents = Object.Instantiate(
            prefabProvider.ChestComponent,
            gameObject.transform
        );
        serverComponents.layer = gameObject.layer;
        resolver.InjectGameObject(serverComponents);

        serverComponents.GetComponent<ChestInteractSystem>().Initialize(chestStateStorage, world);

        RegisterEntity(netId, networkAdapter);

        Debug.Log(
            $"[ServerChestLinker] Linked chest with NetID {netId} and registered in world. server components: {serverComponents != null}"
        );
    }

    public void RegisterEntity(uint netID, NetworkAdapter networkAdapter)
    {
        var entity = new ServerEntity(netID, networkAdapter, null);
        world.Entities.Register(netID, entity);
    }
}
