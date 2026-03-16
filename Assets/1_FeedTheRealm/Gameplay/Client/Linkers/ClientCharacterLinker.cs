using FTR.Core.Client;
using FTR.Gameplay.Client.Characters;
using FTR.Gameplay.Client.Characters.Shared.StateMachine;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Linkers
{
    public class ClientCharacterLinker
    {
        private readonly ClientPrefabProvider prefabProvider;
        private readonly IObjectResolver resolver;
        private readonly NpcDialogRegistry npcDialogRegistry;

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
            var interactController = playerComponents.GetComponent<InteractController>();
            var interactView = playerComponents.GetComponent<InteractView>();

            networkEventRouter.Initialize(networkAdapter);
            movementView.Initialize(rb, stateStorage);
            attackView.Initialize(networkEventRouter);
            dashView.Initialize(rb, stateStorage, networkEventRouter);
            staminaView.Initialize(stateStorage);
            healthView?.Initialize(stateStorage);

            movementController.Initialize(networkAdapter);
            useController.Initialize(networkAdapter);
            interactController.Initialize(networkAdapter);
            interactView.Initialize(networkEventRouter, npcDialogRegistry, stateStorage);

            return playerComponents;
        }
    }
}
