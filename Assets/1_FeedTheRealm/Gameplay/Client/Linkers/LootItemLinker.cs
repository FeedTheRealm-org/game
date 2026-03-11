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
        if (gameObject == null)
            throw new System.ArgumentNullException(nameof(gameObject));
        if (prefabProvider == null)
            throw new System.NullReferenceException("prefabProvider is null.");
        if (prefabProvider.LootItemVisual == null)
            throw new System.NullReferenceException("prefabProvider.LootItemVisual is null.");
        if (resolver == null)
            throw new System.NullReferenceException("resolver is null.");

        var rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null)
            throw new MissingComponentException(
                $"Missing {nameof(Rigidbody)} on '{gameObject.name}'."
            );

        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        if (networkAdapter == null)
            throw new MissingComponentException(
                $"Missing {nameof(NetworkAdapter)} on '{gameObject.name}'."
            );

        var clientLootItemComponents = Object.Instantiate(
            prefabProvider.LootItemVisual,
            gameObject.transform
        );

        if (clientLootItemComponents == null)
            throw new System.NullReferenceException(
                "Instantiate returned null for LootItemVisual."
            );

        resolver.InjectGameObject(clientLootItemComponents);

        var networkEventRouter = clientLootItemComponents.GetComponent<NetworkEventRouter>();
        if (networkEventRouter == null)
            throw new MissingComponentException(
                $"Missing {nameof(NetworkEventRouter)} on instantiated LootItemVisual '{clientLootItemComponents.name}'."
            );

        var lootItemView = clientLootItemComponents.GetComponent<LootItemView>();
        if (lootItemView == null)
            throw new MissingComponentException(
                $"Missing {nameof(LootItemView)} on instantiated LootItemVisual '{clientLootItemComponents.name}'."
            );

        lootItemView.Initialize(rb, networkEventRouter);
        networkEventRouter.Initialize(networkAdapter);
    }
}
