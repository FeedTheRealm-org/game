using FTR.Core.Client;
using FTR.Core.Common.Loaders;
using UnityEngine;

namespace FTR.Gameplay.Client.Characters;

public class ClientCharacterLinker : IScriptLinker
{
    private readonly ClientPrefabProvider prefabProvider;

    public ClientCharacterLinker(ClientPrefabProvider prefabProvider)
    {
        this.prefabProvider = prefabProvider;
    }

    public void LinkDomainScripts(GameObject gameObject)
    {
        var rb = gameObject.GetComponent<Rigidbody>();

        var playerComponents = UnityEngine.Object.Instantiate(
            prefabProvider.ClientPlayerComponents,
            gameObject.transform
        );

        var characterStateMachine = playerComponents.GetComponent<CharacterStateMachine>();
        var movementView = playerComponents.GetComponent<MovementView>();
        movementView.Initialize(rb);

        var playerController = gameObject.AddComponent<PlayerController>();
        playerController.Initialize(characterStateMachine);
    }
}
