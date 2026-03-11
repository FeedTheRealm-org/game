using FTR.Core.Client;
using FTR.Gameplay.Common.Linkers;
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
        // Get from common character components
        var rb = gameObject.GetComponent<Rigidbody>();
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();

        // Add client-side components
        var clientLootItemComponents = Object.Instantiate(
            prefabProvider.LootItemVisual,
            gameObject.transform
        );
        resolver.InjectGameObject(clientLootItemComponents);

        var networkEventRouter = clientLootItemComponents.GetComponent<NetworkEventRouter>();
        var lootItemView = clientLootItemComponents.GetComponent<LootItemView>();
        lootItemView.Initialize(rb, networkEventRouter);
        networkEventRouter.Initialize(networkAdapter);
    }
}
