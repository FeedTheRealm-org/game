using FTR.Core.Server;
using FTR.Core.Server.Entities;
using FTR.Gameplay.Common.Environment.Chests;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Chest;
using FTR.Gameplay.Common.NetworkEntities.Portal;
using FTR.Gameplay.Server.Environment.Chest;
using FTR.Gameplay.Server.Environment.Portal;
using FTR.Gameplay.Server.Registry;
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
        ServerPrefabProvider prefabProvider,
        IObjectResolver resolver,
        WorldMonitor world
    )
    {
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
        this.world = world;
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

        var chestController = gameObject.GetComponent<ChestController>();
        serverComponents
            .GetComponent<ChestInteractSystem>()
            .Initialize(chestController, chestStateStorage);

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
