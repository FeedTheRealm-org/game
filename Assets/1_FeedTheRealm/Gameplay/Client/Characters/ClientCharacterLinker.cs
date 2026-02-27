using FTR.Core.Client;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Common.NetworkEntities.Characters;
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
        // Get from common character components
        var rb = gameObject.GetComponent<Rigidbody>();
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();

        // Add client-side components
        var playerComponents = UnityEngine.Object.Instantiate(
            prefabProvider.ClientPlayerComponents,
            gameObject.transform
        );

        // Initialize components
        var characterStateMachine = playerComponents.GetComponent<CharacterStateMachine>();
        var movementView = playerComponents.GetComponent<MovementView>();
        movementView.Initialize(rb, stateStorage);

        var playerController = gameObject.AddComponent<PlayerController>();
        playerController.Initialize(characterStateMachine);

        var movementController = gameObject.AddComponent<MovementController>();
        movementController.Initialize(networkAdapter);
    }
}
