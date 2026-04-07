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

    public ClientAggresiveNpcLinker(ClientPrefabProvider prefabProvider, IObjectResolver resolver)
    {
        this.characterLinker = new ClientCharacterLinker(prefabProvider, resolver);
        this.npcEnemySpriteRepository = new ClientNpcEnemySpriteRepository();
    }

    public override void Link(GameObject gameObject)
    {
        var characterComponent = characterLinker.Link(gameObject);
        var stateStorage = gameObject.GetComponent<CharacterStateStorage>();
        var spriteLoader = characterComponent.GetComponentInChildren<SpriteLoader>();
        var spriteManager = characterComponent.GetComponentInChildren<SpriteManager>();

        if (stateStorage == null || spriteLoader == null || spriteManager == null)
        {
            Debug.LogWarning(
                "[ClientAggresiveNpcLinker] Missing sprite components or CharacterStateStorage for aggressive NPC."
            );
            return;
        }

        spriteManager.Initialize(spriteLoader, npcEnemySpriteRepository, stateStorage);
    }
}
