using FTR.Core.Client;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Characters;

public class ClientCharacterLinker : IScriptLinker
{
    private readonly ClientPrefabProvider prefabProvider;

    private readonly IObjectResolver resolver;

    public ClientCharacterLinker(ClientPrefabProvider prefabProvider, IObjectResolver resolver)
    {
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;

        Debug.Log("ClientCharacterLinker created with prefabProvider: " + (prefabProvider != null));
        Debug.Log("ClientCharacterLinker created with resolver: " + (resolver != null));
    }

    public void LinkDomainScripts(GameObject gameObject)
    {
        // Get from common character components
        var rb = gameObject.GetComponent<Rigidbody>();
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();

        // Add client-side components
        var playerComponents = Object.Instantiate(
            prefabProvider.ClientPlayerComponents,
            gameObject.transform
        );

        resolver.InjectGameObject(playerComponents);

        var characterStateMachine = playerComponents.GetComponent<CharacterStateMachine>();
        var movementView = playerComponents.GetComponent<MovementView>();
        movementView.Initialize(rb, stateStorage);

        var playerController = gameObject.AddComponent<PlayerController>();
        playerController.Initialize(characterStateMachine);

        var movementController = gameObject.AddComponent<MovementController>();
        movementController.Initialize(networkAdapter);
    }
}
