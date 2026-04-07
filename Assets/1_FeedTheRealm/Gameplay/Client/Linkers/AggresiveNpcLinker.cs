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
    private readonly ClientNpcEnemySpriteRepository npcEnemySpriteRepository;

    public ClientAggresiveNpcLinker(
        ClientPrefabProvider prefabProvider,
        IObjectResolver resolver,
        ClientNpcEnemySpriteRepository npcEnemySpriteRepository
    )
    {
        this.characterLinker = new ClientCharacterLinker(prefabProvider, resolver);
        this.npcEnemySpriteRepository = npcEnemySpriteRepository;
    }

    public override void Link(GameObject gameObject)
    {
        var characterComponent = characterLinker.Link(gameObject);
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
        var spriteLoader = characterComponent.GetComponentInChildren<SpriteLoader>();
        var spriteManager = characterComponent.GetComponentInChildren<SpriteManager>();

        spriteManager.Initialize(spriteLoader, npcEnemySpriteRepository, stateStorage);
    }
}
