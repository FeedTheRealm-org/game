using FTR.Core.Server;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Linkers;

public class ServerPassiveNpcLinker : BaseServerPlayerLinker
{
    public ServerPassiveNpcLinker(
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
