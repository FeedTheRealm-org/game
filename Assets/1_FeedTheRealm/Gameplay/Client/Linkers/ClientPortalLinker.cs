using FTR.Core.Client;
using FTR.Gameplay.Client.Characters.Shared.Portal;
using FTR.Gameplay.Common.Linkers;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Linkers;

public class ClientPortalItemLinker : LootItemLinker
{
    private readonly ClientPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;

    public ClientPortalItemLinker(ClientPrefabProvider prefabProvider, IObjectResolver resolver)
    {
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;

        Debug.Log(
            "ClientPortalItemLinker created with prefabProvider: " + (prefabProvider != null)
        );
        Debug.Log("ClientPortalItemLinker created with resolver: " + (resolver != null));
    }

    public override void Link(GameObject gameObject)
    {
        // Get references to required components
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();

        var clientLootItemComponents = Object.Instantiate(
            prefabProvider.PortalVisual,
            gameObject.transform
        );
        clientLootItemComponents.layer = gameObject.layer;

        resolver.InjectGameObject(clientLootItemComponents);

        // Get references to the client-side components
        var networkEventRouter = clientLootItemComponents.GetComponent<NetworkEventRouter>();
        var portalView = clientLootItemComponents.GetComponent<PortalView>();

        // Initialize components
        portalView.Initialize(networkEventRouter, networkAdapter);
        networkEventRouter.Initialize(networkAdapter);
    }
}
