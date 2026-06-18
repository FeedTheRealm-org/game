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

        public (GameObject Components, ICharacterNameController NameController) Link(
            GameObject gameObject
        )
        {
            var rb = gameObject.GetComponent<Rigidbody>();
            var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
            var networkAdapter = gameObject.GetComponent<NetworkAdapter>();

            var characterComponents = Object.Instantiate(
                prefabProvider.ClientCharacterComponents,
                gameObject.transform
            );
            resolver.InjectGameObject(characterComponents);

            var nameController = SetupNameTag(gameObject, characterComponents);

            var networkEventRouter = characterComponents.GetComponent<NetworkEventRouter>();
            var movementView = characterComponents.GetComponent<MovementView>();
            var useView = characterComponents.GetComponent<UseView>();
            var dashView = characterComponents.GetComponent<DashView>();
            var healthView = characterComponents.GetComponent<HealthView>();
            var spriteManager = characterComponents.GetComponentInChildren<SpriteManager>();
            var movementController = characterComponents.GetComponent<MovementController>();
            var useController = characterComponents.GetComponent<UseController>();

            networkEventRouter.Initialize(networkAdapter);
            movementView.Initialize(rb, stateStorage);
            useView.Initialize(networkEventRouter, stateStorage, spriteManager);
            dashView.Initialize(rb, stateStorage, networkEventRouter);
            healthView?.Initialize(stateStorage);

            movementController.Initialize(networkAdapter);
            useController.Initialize(networkAdapter);

            return (characterComponents, nameController);
        }

        private ICharacterNameController SetupNameTag(
            GameObject gameObject,
            GameObject characterComponents
        )
        {
            var characterBody = characterComponents.transform.Find("CharacterBody");
            var attachParent = characterBody != null ? characterBody : gameObject.transform;

            prefabProvider.NameTagPrefab.SetActive(false);
            var nameTagInstance = Object.Instantiate(prefabProvider.NameTagPrefab, attachParent);
            resolver.InjectGameObject(nameTagInstance);
            nameTagInstance.SetActive(true);

            return nameTagInstance.GetComponent<ICharacterNameController>();
        }
    }
}
