using FTR.Core.Client;
using FTR.Gameplay.Client.Characters.Shared.StateMachine;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Linkers
{
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
            var rb = gameObject.GetComponent<Rigidbody>();
            var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
            var networkAdapter = gameObject.GetComponent<NetworkAdapter>();

            var characterComponents = Object.Instantiate(
                prefabProvider.ClientCharacterComponents,
                gameObject.transform
            );
            resolver.InjectGameObject(characterComponents);
            SetupNameTag(gameObject, characterComponents);

            var networkEventRouter = characterComponents.GetComponent<NetworkEventRouter>();
            var movementView = characterComponents.GetComponent<MovementView>();
            var attackView = characterComponents.GetComponent<AttackView>();
            var dashView = characterComponents.GetComponent<DashView>();
            var staminaView = characterComponents.GetComponent<StaminaView>();
            var healthView = characterComponents.GetComponent<HealthView>();
            var movementController = characterComponents.GetComponent<MovementController>();
            var useController = characterComponents.GetComponent<UseController>();

            networkEventRouter.Initialize(networkAdapter);
            movementView.Initialize(rb, stateStorage);
            attackView.Initialize(networkEventRouter);
            dashView.Initialize(rb, stateStorage, networkEventRouter);
            staminaView.Initialize(stateStorage);
            healthView?.Initialize(stateStorage);

            movementController.Initialize(networkAdapter);
            useController.Initialize(networkAdapter);

            return characterComponents;
        }

        private void SetupNameTag(GameObject gameObject, GameObject characterComponents)
        {
            var characterBody = characterComponents.transform.Find("CharacterBody");
            var attachParent = characterBody != null ? characterBody : gameObject.transform;

            prefabProvider.NameTagPrefab.SetActive(false);
            var nameTagInstance = Object.Instantiate(prefabProvider.NameTagPrefab, attachParent);
            resolver.InjectGameObject(nameTagInstance);
            nameTagInstance.SetActive(true);
        }
    }
}
