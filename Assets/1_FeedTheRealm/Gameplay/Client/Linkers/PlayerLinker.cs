using FTR.Core.Client;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Client.Characters.Player;
using FTR.Gameplay.Client.Characters.Shared.StateMachine;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Characters;
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
    private readonly Session.Session session;

    public ClientPlayerLinker(
        ClientPrefabProvider prefabProvider,
        IObjectResolver resolver,
        NpcDialogRegistry npcDialogRegistry,
        Session.Session session
    )
    {
        this.characterLinker = new ClientCharacterLinker(prefabProvider, resolver);
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
        this.npcDialogRegistry = npcDialogRegistry;
        this.session = session;
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
            var setUserIdTransaction = new TransactionCommandDTO
            {
                Type = TransactionType.SetUserId,
                Id = session.UserId, // TODO: change for TokenID that got from core-service after signaling intent to join a world
                content = null,
            };
            networkAdapter.DispatchTransaction(setUserIdTransaction);

            // var fastSlotState = gameObject.GetComponent<FastSlotStateStorage>();
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

            /* -- Instantiate and initialize controllers and views -- */

            var playerController = gameObject.AddComponent<PlayerController>();

            var inventoryState = gameObject.GetComponent<InventoryStateStorage>();
            var inventoryController = playerComponents.AddComponent<InventoryController>();
            var inventoryView = playerComponents.AddComponent<InventoryView>();
            var interactController = playerComponents.AddComponent<InteractController>();
            var interactView = hudComponent.AddComponent<InteractView>();
            var questView = hudComponent.AddComponent<QuestView>();

            resolver.Inject(playerController);
            resolver.Inject(interactController);
            resolver.Inject(interactView);
            resolver.Inject(questView);
            resolver.Inject(inventoryController);
            resolver.Inject(inventoryView);

            inventoryController.Initialize(networkAdapter);
            inventoryView?.Initialize(inventoryState);
            interactController?.Initialize(networkAdapter);
            questView?.Initialize(networkAdapter);
            characterStateMachine?.Initialize(interactController);
            interactView?.Initialize(networkEventRouter, npcDialogRegistry);
            playerController.Initialize(characterStateMachine);
        }
    }
}
