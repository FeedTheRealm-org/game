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

            SortingLayerSetup(characterComponents);

            var nameController = SetupNameTag(gameObject, characterComponents);

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

        private void SortingLayerSetup(GameObject gameObject)
        {
            // var spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);

            // foreach (var sr in spriteRenderers)
            // {
            //     sr.sortingLayerName = "Characters";
            //     sr.sortingOrder = 0;
            // }

            // var meshRenderers = gameObject.GetComponentsInChildren<Renderer>(true);

            // foreach (var r in meshRenderers)
            // {
            //     if (r is SpriteRenderer) continue;

            //     r.sortingLayerName = "Ground";
            //     r.sortingOrder = -300;
            // }
        }
    }
}
