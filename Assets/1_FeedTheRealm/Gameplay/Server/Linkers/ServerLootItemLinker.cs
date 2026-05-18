using FTR.Core.Server;
using FTR.Core.Server.Entities;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using FTR.Gameplay.Server.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using FTR.Gameplay.Server.Environment.LootItem;
using FTR.Gameplay.Server.Reaper;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Linkers;

public class ServerLootItemLinker : LootItemLinker
{
    private readonly WorldMonitor world;
    private readonly ServerPrefabProvider prefabProvider;
    private readonly EntityReaper reaper;
    private readonly IObjectResolver resolver;

    public ServerLootItemLinker(
        WorldMonitor world,
        ServerPrefabProvider prefabProvider,
        EntityReaper reaper,
        IObjectResolver resolver
    )
    {
        this.world = world;
        this.prefabProvider = prefabProvider;
        this.reaper = reaper;
        this.resolver = resolver;
    }

    public override void Link(GameObject gameObject)
    {
        var rb = gameObject.GetComponent<Rigidbody>();
        var col = gameObject.GetComponent<Collider>();
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        var netId = gameObject.GetComponent<NetworkIdentity>().netId;
        var stateStorage = gameObject.GetComponent<LootItemStateStorage>();

        var tracker = gameObject.AddComponent<ServerEntityCleanupTracker>();
        tracker.Initialize(world, netId);

        // Add server-side components
        var serverComponents = resolver.Instantiate(
            prefabProvider.ServerLootItemComponents,
            gameObject.transform
        );
        serverComponents.layer = gameObject.layer;

        resolver.InjectGameObject(serverComponents);

        var lootItemSystem = serverComponents.GetComponent<LootItemSystem>();
        var lootItemController = serverComponents.GetComponent<LootItemController>();
        var groundCheck = serverComponents.GetComponent<GroundCheckSystem>();

        // Initialize components
        lootItemController.Initialize(netId, stateStorage.ItemId, stateStorage.GoldAmount);
        lootItemSystem.Initialize(rb, netId, stateStorage);
        groundCheck.Initialize(col, stateStorage);

        RegisterEntity(netId, networkAdapter);
        reaper.Register(serverComponents);
    }

    public void RegisterEntity(uint netID, NetworkAdapter networkAdapter)
    {
        var entity = new ServerEntity(netID, networkAdapter, null);
        world.Entities.Register(netID, entity);
    }
}
