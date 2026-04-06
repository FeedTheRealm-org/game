using FTR.Core.Client;
using FTR.Gameplay.Client.Characters.Shared.StateMachine;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.NetworkEntities.Gold;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Linkers;

public class ClientShopLinker : ShopLinker
{
    private readonly ClientPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;

    public ClientShopLinker(ClientPrefabProvider prefabProvider, IObjectResolver resolver)
    {
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
    }

    public override void Link(GameObject gameObject)
    {
        Debug.Log(
            "[ClientShopLinker] Linking shop with netId: "
                + gameObject.GetComponent<Mirror.NetworkIdentity>().netId
        );

        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        if (networkAdapter == null)
        {
            Debug.LogWarning(
                "[ClientShopLinker] NetworkAdapter component is missing on shop object."
            );
            return;
        }

        var clientShopComponents = Object.Instantiate(
            prefabProvider.ShopItemVisual,
            gameObject.transform
        );
        clientShopComponents.layer = gameObject.layer;

        var eventRouter = clientShopComponents.GetComponent<NetworkEventRouter>();

        resolver.InjectGameObject(clientShopComponents);
        var shopView = clientShopComponents.GetComponent<ShopView>();

        shopView.Initialize(eventRouter);
        eventRouter.Initialize(networkAdapter);
    }
}
