using FTR.Core.Client;
using FTR.Gameplay.Common.Linkers;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Client.Linkers;

public class ClientPassiveNpcLinker : PassiveNpcLinker
{
    private ClientCharacterLinker characterLinker;

    public ClientPassiveNpcLinker(ClientPrefabProvider prefabProvider, IObjectResolver resolver)
    {
        this.characterLinker = new ClientCharacterLinker(prefabProvider, resolver);
    }

    public override void Link(GameObject gameObject)
    {
        characterLinker.Link(gameObject);
    }
}
