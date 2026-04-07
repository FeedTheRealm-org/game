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
    private readonly ClientNpcEnemySpriteRepository npcEnemySpriteRepository;
    private readonly ClientPrefabProvider prefabProvider;
    private readonly IObjectResolver resolver;
    private readonly NpcDialogRegistry npcDialogRegistry;

    public ClientPassiveNpcLinker(
        ClientPrefabProvider prefabProvider,
        IObjectResolver resolver,
        NpcDialogRegistry npcDialogRegistry
    )
    {
        this.characterLinker = new ClientCharacterLinker(prefabProvider, resolver);
        this.npcEnemySpriteRepository = new ClientNpcEnemySpriteRepository();
        this.prefabProvider = prefabProvider;
        this.resolver = resolver;
        this.npcDialogRegistry = npcDialogRegistry;
    }

    public override void Link(GameObject gameObject)
    {
        var characterComponent = characterLinker.Link(gameObject);
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
        var spriteLoader = characterComponent.GetComponentInChildren<SpriteLoader>();
        var spriteManager = characterComponent.GetComponentInChildren<SpriteManager>();

        if (stateStorage != null && spriteLoader != null && spriteManager != null)
        {
            spriteManager.Initialize(spriteLoader, npcEnemySpriteRepository, stateStorage);
        }
        else
        {
            Debug.LogWarning(
                "[ClientPassiveNpcLinker] Missing sprite components or CharacterStateStorage for passive NPC."
            );
        }

        var characterBody = characterComponent.transform.Find("CharacterBody");
        var dialogParent = characterBody != null ? characterBody : gameObject.transform;

        var networkEventRouter = characterComponent.GetComponent<NetworkEventRouter>();
        var interactView = characterComponent.AddComponent<InteractView>();
        resolver.Inject(interactView);
        interactView.Initialize(networkEventRouter, npcDialogRegistry);

        prefabProvider.DialogBox.SetActive(false);
        var dialogBoxComponent = Object.Instantiate(prefabProvider.DialogBox, dialogParent);
        dialogBoxComponent.transform.localPosition = new Vector3(1f, -0.2f, 0);
        dialogBoxComponent.transform.localScale = new Vector3(0.4f, 0.4f, 0);
        resolver.InjectGameObject(dialogBoxComponent);
        dialogBoxComponent.SetActive(true);
    }
}
