using FTR.Core.Server;
using FTR.Gameplay.Common.Linkers;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Linkers;

public class ServerPlayerLinker : PlayerLinker
{
    private ServerCharacterLinker characterLinker;

    public ServerPlayerLinker(
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
        gameObject.name = $"Player";
    }
}
