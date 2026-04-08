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

            var characterComponent = Object.Instantiate(
                prefabProvider.ClientCharacterComponents,
                gameObject.transform
            );
            resolver.InjectGameObject(characterComponent);
            SetupNameTag(gameObject, characterComponent);

            var networkEventRouter = characterComponent.GetComponent<NetworkEventRouter>();
            var movementView = characterComponent.GetComponent<MovementView>();
            var attackView = characterComponent.GetComponent<AttackView>();
            var dashView = characterComponent.GetComponent<DashView>();
            var staminaView = characterComponent.GetComponent<StaminaView>();
            var healthView = characterComponent.GetComponent<HealthView>();
            var movementController = characterComponent.GetComponent<MovementController>();
            var useController = characterComponent.GetComponent<UseController>();

            networkEventRouter.Initialize(networkAdapter);
            movementView.Initialize(rb, stateStorage);
            attackView.Initialize(networkEventRouter);
            dashView.Initialize(rb, stateStorage, networkEventRouter);
            staminaView.Initialize(stateStorage);
            healthView?.Initialize(stateStorage);

            movementController.Initialize(networkAdapter);
            useController.Initialize(networkAdapter);

            return characterComponent;
        }

        private void SetupNameTag(GameObject gameObject, GameObject characterComponent)
        {
            var characterBody = characterComponent.transform.Find("CharacterBody");
            var attachParent = characterBody != null ? characterBody : gameObject.transform;

            prefabProvider.NameTagPrefab.SetActive(false);
            var nameTagInstance = Object.Instantiate(prefabProvider.NameTagPrefab, attachParent);
            resolver.InjectGameObject(nameTagInstance);
            nameTagInstance.SetActive(true);
        }
    }
}
