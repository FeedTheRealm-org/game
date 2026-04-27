using FTR.Core.Client;
using FTR.Core.Common.Utils;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Linkers;

public class ClientLootItemLinker : LootItemLinker
{
    private readonly ClientPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;

    public ClientLootItemLinker(ClientPrefabProvider prefabProvider, IObjectResolver resolver)
    {
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;

        Debug.Log("ClientLootItemLinker created with prefabProvider: " + (prefabProvider != null));
        Debug.Log("ClientLootItemLinker created with resolver: " + (resolver != null));
    }

    public override void Link(GameObject gameObject)
    {
        // Get references to required components
        var rb = gameObject.GetComponent<Rigidbody>();
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        var stateStorage = gameObject.GetComponent<LootItemStateStorage>();

        var clientLootItemComponents = Object.Instantiate(
            prefabProvider.LootItemVisual,
            gameObject.transform
        );
        clientLootItemComponents.layer = gameObject.layer;

        resolver.InjectGameObject(clientLootItemComponents);

        // Get references to the client-side components
        var networkEventRouter = clientLootItemComponents.GetComponent<NetworkEventRouter>();
        var lootItemView = clientLootItemComponents.GetComponent<LootItemView>();

        // Initialize components
        lootItemView.Initialize(rb, networkEventRouter, stateStorage);
        networkEventRouter.Initialize(networkAdapter);
    }
}
