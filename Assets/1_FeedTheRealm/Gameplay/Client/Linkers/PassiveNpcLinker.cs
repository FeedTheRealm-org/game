using FTR.Core.Client;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.Linkers;

public class ClientPassiveNpcLinker : PassiveNpcLinker
{
    private readonly ClientCharacterLinker characterLinker;
    private readonly ClientNpcInfoRepository npcInfoRepository;
    private readonly ClientPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;
    private readonly NpcDialogRegistry npcDialogRegistry;

    public ClientPassiveNpcLinker(
        ClientPrefabProvider prefabProvider,
        IObjectResolver resolver,
        NpcDialogRegistry npcDialogRegistry,
        ClientNpcInfoRepository npcInfoRepository
    )
    {
        this.characterLinker = new ClientCharacterLinker(prefabProvider, resolver);
        this.npcInfoRepository = npcInfoRepository;
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
        this.npcDialogRegistry = npcDialogRegistry;
    }

    public override void Link(GameObject gameObject)
    {
        var (characterComponent, nameController) = characterLinker.Link(gameObject);
        var characterBody = characterComponent.transform.Find("CharacterBody");
        var attachParent = characterBody != null ? characterBody : gameObject.transform;

        var networkEventRouter = characterComponent.GetComponent<NetworkEventRouter>();
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
        var spriteLoader = characterComponent.GetComponentInChildren<SpriteLoader>();
        var spriteManager = characterComponent.GetComponentInChildren<SpriteManager>();
        var interactView = characterComponent.AddComponent<InteractView>();

        resolver.Inject(interactView);

        spriteManager.Initialize(spriteLoader, npcInfoRepository, stateStorage, nameController);
        interactView.Initialize(networkEventRouter, npcDialogRegistry);

        prefabProvider.DialogBox.SetActive(false);
        var dialogBoxComponent = Object.Instantiate(prefabProvider.DialogBox, attachParent);
        resolver.InjectGameObject(dialogBoxComponent);
        dialogBoxComponent.SetActive(true);

        prefabProvider.QuestSignPrefab.SetActive(false);
        var questSignInstance = Object.Instantiate(prefabProvider.QuestSignPrefab, attachParent);
        questSignInstance.transform.localPosition = new Vector3(0, 1.4f, 0);
        questSignInstance.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        resolver.InjectGameObject(questSignInstance);

        prefabProvider.QuestHaloPrefab.SetActive(false);
        var questHaloInstance = Object.Instantiate(prefabProvider.QuestHaloPrefab, attachParent);
        questHaloInstance.transform.localPosition = new Vector3(0, 0, 0);
        questHaloInstance.transform.localScale = new Vector3(4, 4, 4);
        resolver.InjectGameObject(questHaloInstance);

        var questSignView = characterComponent.AddComponent<QuestSignView>();
        resolver.Inject(questSignView);
        questSignView.Initialize(
            stateStorage.CharacterId,
            npcDialogRegistry,
            questSignInstance,
            questHaloInstance
        );

        resolver.Inject(interactView);
        interactView.Initialize(networkEventRouter, npcDialogRegistry);
    }
}
