using FeedTheRealm.Core.Interfaces;
using FTR.Core.Client;
using FTR.Core.Client.EntryPoints;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Gameplay.Client.Characters.Player;
using FTR.Gameplay.Client.Characters.Shared.StateMachine;
using FTR.Gameplay.Client.EntryPoints;
using FTR.Gameplay.Common.Characters.Shared.Portal;
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
    private readonly Session.Session session;
    private readonly WorldSelector worldSelector;
    private readonly PlayerInfoRepository playerInfoRepository;

    public ClientPlayerLinker(
        ClientPrefabProvider prefabProvider,
        IObjectResolver resolver,
        NpcDialogRegistry npcDialogRegistry,
        Session.Session session,
        WorldSelector worldSelector,
        PlayerInfoRepository playerInfoRepository
    )
    {
        this.characterLinker = new ClientCharacterLinker(prefabProvider, resolver);
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
        this.npcDialogRegistry = npcDialogRegistry;
        this.session = session;
        this.worldSelector = worldSelector;
        this.playerInfoRepository = playerInfoRepository;
    }

    public override void Link(GameObject gameObject)
    {
        var (playerComponents, nameController) = characterLinker.Link(gameObject);

        var networkAdapter = gameObject.GetComponent<NetworkAdapter>();
        if (networkAdapter == null)
        {
            Debug.LogWarning(
                "[ClientPlayerLinker] NetworkAdapter component is missing on player object."
            );
            return;
        }
        var networkEventRouter = playerComponents.GetComponent<NetworkEventRouter>();

        var characterStateMachine = playerComponents.GetComponent<CharacterStateMachine>();
        var spriteLoader = playerComponents.GetComponentInChildren<SpriteLoader>();
        var spriteManager = playerComponents.GetComponentInChildren<SpriteManager>();
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();

        var characterBody = playerComponents.transform.Find("CharacterBody");
        var attachParent = characterBody != null ? characterBody : gameObject.transform;

        spriteManager.Initialize(
            spriteLoader,
            playerInfoRepository,
            stateStorage,
            nameController,
            networkAdapter.IsLocalPlayer
        );

        // Initialize chat box
        prefabProvider.ChatBox.SetActive(false);
        var chatBoxComponent = Object.Instantiate(prefabProvider.ChatBox, attachParent);
        resolver.InjectGameObject(chatBoxComponent);
        chatBoxComponent.SetActive(true);

        var chatView = playerComponents.AddComponent<ChatView>();

        resolver.Inject(chatView);
        chatView.Initialize(
            networkEventRouter,
            networkAdapter.netId,
            chatBoxComponent.GetComponent<IChatBox>()
        );

        if (networkAdapter.IsLocalPlayer)
        {
            var joinToken = worldSelector?.GetSelectedWorldJoinToken();
            var setUserIdTransaction = new TransactionCommandDTO
            {
                Type = TransactionType.SetUserId,
                Id = joinToken,
                content = null,
            };
            networkAdapter.DispatchTransaction(setUserIdTransaction);

            var soundPlayer = resolver.Resolve<ISoundPlayer>();
            soundPlayer.Play(
                Registry.ClientSoundFXRegistry.SoundFXIds.Spawn,
                gameObject.transform.position
            );

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

            prefabProvider.PortalVisual.SetActive(false);
            var portalVisual = resolver.Instantiate(
                prefabProvider.PortalVisual,
                gameObject.transform
            );
            portalVisual.SetActive(true);

            prefabProvider.ChatInput.SetActive(false);
            var chatInput = Object.Instantiate(prefabProvider.ChatInput, gameObject.transform);
            resolver.InjectGameObject(chatInput);
            chatInput.SetActive(true);

            /* -- Instantiate and initialize controllers and views -- */

            var playerController = gameObject.AddComponent<PlayerController>();

            var inventoryState = gameObject.GetComponent<InventoryStateStorage>();
            var goldState = gameObject.GetComponent<GoldStateStorage>();
            var inventoryController = playerComponents.AddComponent<InventoryController>();
            var inventoryView = playerComponents.AddComponent<InventoryView>();
            var useView = playerComponents.GetComponent<UseView>();
            var interactController = playerComponents.AddComponent<InteractController>();
            var interactView = hudComponent.AddComponent<InteractView>();
            var questView = hudComponent.AddComponent<QuestView>();
            var questProgressView = hudComponent.AddComponent<QuestProgressView>();

            var goldController = playerComponents.AddComponent<GoldController>();
            var goldView = playerComponents.AddComponent<GoldView>();

            var portalView = playerComponents.GetComponent<PortalView>();
            var chatController = playerComponents.AddComponent<ChatController>();

            resolver.Inject(playerController);
            resolver.Inject(interactController);
            resolver.Inject(interactView);
            resolver.Inject(questView);
            resolver.Inject(inventoryController);
            resolver.Inject(inventoryView);

            resolver.Inject(goldView);
            resolver.Inject(goldController);
            resolver.Inject(portalView);

            resolver.Inject(chatController);

            inventoryController.Initialize(networkAdapter);
            inventoryView?.Initialize(inventoryState, networkEventRouter);
            goldView?.Initialize(goldState, networkEventRouter);
            goldController?.Initialize(networkAdapter);
            interactController?.Initialize(networkAdapter);
            questView?.Initialize(networkAdapter);
            resolver.Inject(questProgressView);
            questProgressView?.Initialize(networkEventRouter);
            characterStateMachine?.Initialize(interactController);
            interactView?.Initialize(networkEventRouter, npcDialogRegistry);
            playerController.Initialize(characterStateMachine);
            portalView?.Initialize(networkEventRouter, networkAdapter);
            chatController.Initialize(networkAdapter);

            useView.SetRangedTargetIndicator(prefabProvider.RangedTargetIndicator);
        }
    }
}
