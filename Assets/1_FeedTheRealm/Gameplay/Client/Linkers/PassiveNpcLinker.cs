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
        var characterComponent = characterLinker.Link(gameObject);
        var characterBody = characterComponent.transform.Find("CharacterBody");

        var dialogParent = characterBody != null ? characterBody : gameObject.transform;

        prefabProvider.DialogBox.SetActive(false);
        var dialogBoxComponent = Object.Instantiate(prefabProvider.DialogBox, dialogParent);
        dialogBoxComponent.transform.localPosition = new Vector3(1f, -0.2f, 0);
        dialogBoxComponent.transform.localScale = new Vector3(0.4f, 0.4f, 0);
        resolver.InjectGameObject(dialogBoxComponent);
        dialogBoxComponent.SetActive(true);
    }
}
