using FTR.Core.Client;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Linkers;

public class ClientPassiveNpcLinker : PassiveNpcLinker
{
    private ClientCharacterLinker characterLinker;
    private readonly ClientPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;

    public ClientPassiveNpcLinker(ClientPrefabProvider prefabProvider, IObjectResolver resolver)
    {
        this.characterLinker = new ClientCharacterLinker(prefabProvider, resolver);
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
    }

    public override void Link(GameObject gameObject)
    {
        characterLinker.Link(gameObject);

        prefabProvider.DialogBox.SetActive(false);
        var dialogBoxComponent = Object.Instantiate(prefabProvider.DialogBox, gameObject.transform);
        dialogBoxComponent.transform.localPosition = new Vector3(0, 2.5f, 0);
        dialogBoxComponent.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        resolver.InjectGameObject(dialogBoxComponent);
        dialogBoxComponent.SetActive(true);
    }
}
