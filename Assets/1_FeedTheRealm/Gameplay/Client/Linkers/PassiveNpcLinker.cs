using FeedTheRealm.Core.Interfaces;
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

        spriteManager.Initialize(spriteLoader, npcInfoRepository, stateStorage, nameController);

        // TODO(refactor): why instantiate it here if its already in the character body as dialog box?
        // Initialize dialog box
        prefabProvider.DialogBox.SetActive(false);
        var dialogBoxComponent = Object.Instantiate(prefabProvider.DialogBox, attachParent);
        resolver.InjectGameObject(dialogBoxComponent);
        dialogBoxComponent.SetActive(true);
        var dialogBox = dialogBoxComponent.GetComponent<IDialogBox>();

        var questSignView = characterComponent.AddComponent<QuestSignView>();
        resolver.Inject(questSignView);
        questSignView.Initialize(stateStorage.CharacterId, npcDialogRegistry, characterBody);

        resolver.Inject(interactView);
        interactView.Initialize(networkEventRouter, npcDialogRegistry, dialogBox);
    }
}
