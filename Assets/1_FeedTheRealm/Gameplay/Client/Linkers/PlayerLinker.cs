using FTR.Core.Client;
using FTR.Gameplay.Common.Linkers;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Linkers;

public class ClientPlayerLinker : PlayerLinker
{
    private readonly ClientPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;
    private ClientCharacterLinker characterLinker;

    public ClientPlayerLinker(ClientPrefabProvider prefabProvider, IObjectResolver resolver)
    {
        this.characterLinker = new ClientCharacterLinker(prefabProvider, resolver);
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
    }

    public override void Link(GameObject gameObject)
    {
        var playerComponents = characterLinker.Link(gameObject);

        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        var characterStateMachine = playerComponents.GetComponent<CharacterStateMachine>();

        if (networkAdapter.IsLocalPlayer)
        {
            prefabProvider.HudComponent.SetActive(false);
            var hudComponent = Object.Instantiate(
                prefabProvider.HudComponent,
                gameObject.transform
            );
            resolver.InjectGameObject(hudComponent);
            hudComponent.SetActive(true);

            var playerController = gameObject.AddComponent<PlayerController>();
            resolver.Inject(playerController);
            playerController.Initialize(characterStateMachine);
        }
    }
}
