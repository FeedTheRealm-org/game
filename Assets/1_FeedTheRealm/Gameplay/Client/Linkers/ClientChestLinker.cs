using FTR.Core.Client;
using FTR.Gameplay.Common.Environment.Chests;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Chest;
using FTR.Gameplay.Environment.Chest;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Linkers;

public class ClientChestLinker : ChestLinker
{
    private readonly ClientPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;

    public ClientChestLinker(ClientPrefabProvider prefabProvider, IObjectResolver resolver)
    {
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
    }

    public override void Link(GameObject gameObject)
    {
        var clientPortalComponents = Object.Instantiate(
            prefabProvider.chestLinkComponents,
            gameObject.transform
        );
        var chestView = clientPortalComponents.GetComponent<ChestView>();
        var chestController = gameObject.GetComponent<ChestController>();
        var chestStateStorage = gameObject.GetComponent<ChestStateStorage>();
        resolver.InjectGameObject(clientPortalComponents);
        chestController.Initialize(chestStateStorage);
        _ = chestView.Initialize(chestStateStorage);
    }
}
