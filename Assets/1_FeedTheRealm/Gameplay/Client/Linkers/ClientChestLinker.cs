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
        var clientChestComponents = Object.Instantiate(
            prefabProvider.chestLinkComponents,
            gameObject.transform
        );
        var chestView = clientChestComponents.GetComponent<ChestView>();
        var chestController = gameObject.GetComponent<ChestController>();
        var chestEffectView = clientChestComponents.GetComponent<ChestEffectView>();
        var chestStateStorage = gameObject.GetComponent<ChestStateStorage>();
        resolver.InjectGameObject(clientChestComponents);
        chestController.Initialize(chestStateStorage);
        _ = chestView.Initialize(chestStateStorage);
    }
}
