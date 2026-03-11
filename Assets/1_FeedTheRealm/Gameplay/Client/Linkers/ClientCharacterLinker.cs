using FTR.Core.Client;
using FTR.Gameplay.Client.Characters;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Linkers;

public class ClientCharacterLinker
{
    private readonly ClientPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;

    public ClientCharacterLinker(ClientPrefabProvider prefabProvider, IObjectResolver resolver)
    {
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
    }

    public GameObject Link(GameObject gameObject)
    {
        // Get from common character components
        var rb = gameObject.GetComponent<Rigidbody>();
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();

        // Add client-side components
        var playerComponents = Object.Instantiate(
            prefabProvider.ClientCharacterComponents,
            gameObject.transform
        );
        resolver.InjectGameObject(playerComponents);

        var characterStateMachine = playerComponents.GetComponent<CharacterStateMachine>();
        var networkEventRouter = playerComponents.GetComponent<NetworkEventRouter>();
        var movementView = playerComponents.GetComponent<MovementView>();
        var attackView = playerComponents.GetComponent<AttackView>();
        var dashView = playerComponents.GetComponent<DashView>();
        var staminaView = playerComponents.GetComponent<StaminaView>();
        var healthView = playerComponents.GetComponent<HealthView>();

        var movementController = playerComponents.GetComponent<MovementController>();
        var useController = playerComponents.GetComponent<UseController>();

        networkEventRouter.Initialize(networkAdapter);
        movementView.Initialize(rb, stateStorage);
        attackView.Initialize(networkEventRouter);
        dashView.Initialize(rb, stateStorage, networkEventRouter);
        staminaView.Initialize(stateStorage);
        healthView?.Initialize(stateStorage);

        movementController.Initialize(networkAdapter);
        useController.Initialize(networkAdapter);

        return playerComponents;
    }
}
