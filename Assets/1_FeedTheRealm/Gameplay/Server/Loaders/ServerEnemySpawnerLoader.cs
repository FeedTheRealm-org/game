using System.Collections.Generic;
using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTRShared.Runtime.Models;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Server.Environment.Loaders;

public class EnemySpawnerLoaderController : ILoader
{
    private WorldData worldData;
    private GameObject spawnerPrefab;
    private Logging.Logger logger;

    public EnemySpawnerLoaderController(
        WorldData worldData,
        GameObject spawnerPrefab,
        Logging.Logger logger
    )
    {
        this.worldData = worldData;
        this.spawnerPrefab = spawnerPrefab;
        this.logger = logger;
    }

    public async UniTask<WorldData> Load()
    {
        var spawnAreas = worldData.enemySpawnAreas;
        if (spawnAreas == null || spawnAreas.Count == 0)
        {
            logger.Log("No enemy spawn areas found; skipping enemy spawner setup.");
            return worldData;
        }

        logger.Log("Setting up enemy spawners | Amount " + spawnAreas.Count);
        foreach (EnemySpawnerData data in spawnAreas)
        {
            GameObject instance = UnityEngine.Object.Instantiate(
                spawnerPrefab,
                new Vector3(data.Position.x, data.Position.y, data.Position.z),
                Quaternion.identity
            );
            EnemySpawn enemySpawnData = instance.GetComponent<EnemySpawn>();
            enemySpawnData.SetupSpawner(data);
            instance.name = "EnemySpawner";
        }
        logger.Log("Finished setting up enemy spawners!");

        return worldData;
    }
}
