using FTR.Core.Server;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Linkers;

public class ServerPlayerLinker : BaseServerPlayerLinker
{
    public ServerPlayerLinker(
        WorldMonitor world,
        ServerPrefabProvider prefabProvider,
        IObjectResolver resolver
    )
        : base(world, prefabProvider, resolver) { }

    public override void Link(GameObject gameObject)
    {
        base.Link(gameObject);
    }
}
