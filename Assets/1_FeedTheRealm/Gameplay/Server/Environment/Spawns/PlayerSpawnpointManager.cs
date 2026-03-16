using System.Collections.Generic;
using FTRShared.Runtime.Models;
using UnityEngine;

public class PlayerSpawnpointManager
{
    private List<PlayerSpawnerData> spawnpoints;

    public PlayerSpawnpointManager()
    {
        spawnpoints = new List<PlayerSpawnerData>();
    }

    public void SetSpawnpoints(List<PlayerSpawnerData> spawnpoints)
    {
        this.spawnpoints = spawnpoints;
    }

    public Vector3 GetRandomSpawnpoint()
    {
        if (spawnpoints == null || spawnpoints.Count == 0)
            return Vector3.zero;

        int index = Random.Range(0, spawnpoints.Count);
        return spawnpoints[index].Position;
    }
}
