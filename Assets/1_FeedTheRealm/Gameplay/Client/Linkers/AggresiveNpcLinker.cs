using FTR.Core.Client;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Client.Linkers;

public class ClientAggresiveNpcLinker : AggresiveNpcLinker
{
    private readonly ClientCharacterLinker characterLinker;
    private readonly ClientNpcInfoRepository npcEnemyInfoRepository;

    public ClientAggresiveNpcLinker(
        ClientPrefabProvider prefabProvider,
        IObjectResolver resolver,
        ClientNpcInfoRepository npcEnemyInfoRepository
    )
    {
        this.characterLinker = new ClientCharacterLinker(prefabProvider, resolver);
        this.npcEnemyInfoRepository = npcEnemyInfoRepository;
    }

    public override void Link(GameObject gameObject)
    {
        var (characterComponent, nameController) = characterLinker.Link(gameObject);
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
        var spriteLoader = characterComponent.GetComponentInChildren<SpriteLoader>();
        var spriteManager = characterComponent.GetComponentInChildren<SpriteManager>();

        spriteManager.Initialize(
            spriteLoader,
            npcEnemyInfoRepository,
            stateStorage,
            nameController
        );
    }
}
