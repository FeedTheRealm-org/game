using FTR.Core.Server;
using FTR.Gameplay.Common.Linkers;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Linkers;

public class ServerPassiveNpcLinker : PassiveNpcLinker
{
    private ServerCharacterLinker characterLinker;

    public ServerPassiveNpcLinker(
        WorldMonitor world,
        ServerPrefabProvider prefabProvider,
        IObjectResolver resolver
    )
    {
        this.characterLinker = new ServerCharacterLinker(world, prefabProvider, resolver);
    }

    public override void Link(GameObject gameObject)
    {
        characterLinker.Link(gameObject);
    }
}
