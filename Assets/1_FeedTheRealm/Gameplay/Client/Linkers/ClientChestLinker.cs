using FTR.Core.Client;
using FTR.Gameplay.Common.Characters.Shared.Portal;
using FTR.Gameplay.Common.Linkers;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Linkers;

public class ClientChestLinker : PortalLinker
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
        clientPortalComponents.layer = gameObject.layer;
        resolver.InjectGameObject(clientPortalComponents);
    }
}
