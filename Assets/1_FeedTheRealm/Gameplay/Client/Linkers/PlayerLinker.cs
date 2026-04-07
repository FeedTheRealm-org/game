using FTR.Core.Client;
using FTR.Gameplay.Client.Characters.Player;
using FTR.Gameplay.Client.Characters.Shared.StateMachine;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.NetworkEntities.Gold;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Linkers;

public class ClientPlayerLinker : PlayerLinker
{
    private readonly ClientPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;
    private readonly ClientCharacterLinker characterLinker;
    private readonly NpcDialogRegistry npcDialogRegistry;

    public ClientPlayerLinker(
        ClientPrefabProvider prefabProvider,
        IObjectResolver resolver,
        NpcDialogRegistry npcDialogRegistry
    )
    {
        this.characterLinker = new ClientCharacterLinker(prefabProvider, resolver);
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
        this.npcDialogRegistry = npcDialogRegistry;
    }

    public override void Link(GameObject gameObject)
    {
        var playerComponents = characterLinker.Link(gameObject);

        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        if (networkAdapter == null)
        {
            Debug.LogWarning(
                "[ClientPlayerLinker] NetworkAdapter component is missing on player object."
            );
            return;
        }

        var characterStateMachine = playerComponents.GetComponent<CharacterStateMachine>();

        if (networkAdapter.IsLocalPlayer)
        {
            var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
            var networkEventRouter = playerComponents.GetComponent<NetworkEventRouter>();

            /* -- Instantiate and inject UI components -- */

            prefabProvider.HudComponent.SetActive(false);
            var hudComponent = Object.Instantiate(
                prefabProvider.HudComponent,
                gameObject.transform
            );
            resolver.InjectGameObject(hudComponent);
            hudComponent.SetActive(true);

            prefabProvider.InventoryHudComponent.SetActive(false);
            var inventoryHudComponent = Object.Instantiate(
                prefabProvider.InventoryHudComponent,
                gameObject.transform
            );

            prefabProvider.QuestPrompt.SetActive(false);
            var questPrompt = Object.Instantiate(prefabProvider.QuestPrompt, gameObject.transform);
            resolver.InjectGameObject(questPrompt);
            questPrompt.SetActive(true);

            prefabProvider.QuestCompletionPanel.SetActive(false);
            var questCompletion = Object.Instantiate(
                prefabProvider.QuestCompletionPanel,
                gameObject.transform
            );
            resolver.InjectGameObject(questCompletion);
            questCompletion.SetActive(true);

            resolver.InjectGameObject(inventoryHudComponent);
            inventoryHudComponent.SetActive(true);

            prefabProvider.ShopMenuComponent.SetActive(false);
            var shopMenuComponent = Object.Instantiate(
                prefabProvider.ShopMenuComponent,
                gameObject.transform
            );
            resolver.InjectGameObject(shopMenuComponent);
            shopMenuComponent.SetActive(true);

            /* -- Instantiate and initialize controllers and views -- */

            var playerController = gameObject.AddComponent<PlayerController>();

            var inventoryState = gameObject.GetComponent<InventoryStateStorage>();
            var goldState = gameObject.GetComponent<GoldStateStorage>();
            var inventoryController = playerComponents.AddComponent<InventoryController>();
            var inventoryView = playerComponents.AddComponent<InventoryView>();
            var interactController = playerComponents.AddComponent<InteractController>();
            var interactView = hudComponent.AddComponent<InteractView>();
            var questView = hudComponent.AddComponent<QuestView>();
            var questProgressView = hudComponent.AddComponent<QuestProgressView>();

            var goldController = playerComponents.AddComponent<GoldController>();
            var goldView = playerComponents.AddComponent<GoldView>();

            resolver.Inject(playerController);
            resolver.Inject(interactController);
            resolver.Inject(interactView);
            resolver.Inject(questView);
            resolver.Inject(inventoryController);
            resolver.Inject(inventoryView);

            resolver.Inject(goldView);
            resolver.Inject(goldController);

            inventoryController.Initialize(networkAdapter);
            inventoryView?.Initialize(inventoryState);
            goldView?.Initialize(goldState, networkEventRouter);
            goldController?.Initialize(networkAdapter);
            interactController?.Initialize(networkAdapter);
            questView?.Initialize(networkAdapter);
            resolver.Inject(questProgressView);
            questProgressView?.Initialize(networkEventRouter);
            characterStateMachine?.Initialize(interactController);
            interactView?.Initialize(networkEventRouter, npcDialogRegistry);
            playerController.Initialize(characterStateMachine);
        }
    }
}
