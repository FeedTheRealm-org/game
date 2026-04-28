using System.Collections.Generic;
using UnityEngine;

namespace FTR.Core.Common.Loaders;

public sealed class NetworkSpawnPendingObjectsRegistry
{
    private readonly List<GameObject> objects = new();

    public void Register(GameObject obj) => objects.Add(obj);

    public void SpawnAll()
    {
        foreach (var obj in objects)
        {
            Mirror.NetworkServer.Spawn(obj);
        }
        objects.Clear();
    }
}
