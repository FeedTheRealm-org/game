using System.Collections.Generic;
using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using Mirror;
using Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Environment.Loaders;

public class NPCSpawnerLoaderController : ILoader
{
    private WorldData worldData;
    private GameObject spawnerPrefab;
    private Logging.Logger logger;

    public NPCSpawnerLoaderController(
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
        var spawnAreas = worldData.npcSpawnAreas;
        if (spawnAreas == null || spawnAreas.Count == 0)
        {
            logger.Log("No NPC spawn areas found; skipping NPC spawner setup.");
            return worldData;
        }

        logger.Log("Setting up NPC spawners | Amount " + spawnAreas.Count);
        foreach (NPCSpawnerData data in spawnAreas)
        {
            GameObject instance = UnityEngine.Object.Instantiate(
                spawnerPrefab,
                new Vector3(data.Position.x, data.Position.y, data.Position.z),
                Quaternion.identity
            );
            NPCSpawns npcSpawnData = instance.GetComponent<NPCSpawns>();
            npcSpawnData.ConfigureFromSpawnData(data, null); // TODO: dialog is missing, add later when ready
            instance.name = "NPCSpawner";

            // when dialog data is available, check if it should be called spawn function here after configuration
        }
        logger.Log("Finished setting up NPC spawners!");
        return worldData;
    }
}
