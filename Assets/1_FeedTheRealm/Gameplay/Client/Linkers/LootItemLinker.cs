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
        var networkEventRouter = gameObject.GetComponent<NetworkEventRouter>();

        // Add client-side components
        var lootItemVIsualComponent = Object.Instantiate(
            prefabProvider.LootItemVisual,
            gameObject.transform
        );
        resolver.InjectGameObject(lootItemVIsualComponent);

        var lootItemView = lootItemVIsualComponent.GetComponent<LootItemView>();
        lootItemView.Initialize(rb, networkEventRouter);

        lootItemVIsualComponent.transform.SetParent(gameObject.transform);
    }
}
