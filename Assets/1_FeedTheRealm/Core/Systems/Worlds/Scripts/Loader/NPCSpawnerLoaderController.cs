using System.Collections.Generic;
using System.Threading.Tasks;
using API;
using Mirror;
using Models;
using UnityEngine;

namespace Core.Systems.Worlds.Loader
{
    public class NPCSpawnerLoaderController : MonoBehaviour, IServerLoader
    {
        [Header("Logger")]
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private GameObject npcSpawnerPrefab;

        public async Task LoadServer(WorldData worldData, string accessToken)
        {
            if (worldData == null)
            {
                logger.Log("No world data provided; skipping NPC spawner setup.", this);
                return;
            }
            var spawnAreas = worldData.npcSpawnAreas;
            if (spawnAreas == null || spawnAreas.Count == 0)
            {
                logger.Log("No NPC spawn areas found; skipping NPC spawner setup.", this);
                return;
            }
            if (npcSpawnerPrefab == null)
            {
                logger.Log("npcSpawnerPrefab is not assigned; cannot create NPC spawners.", this);
                return;
            }
            logger.Log("Setting up NPC spawners | Amount " + spawnAreas.Count, this);
            foreach (NPCSpawnerData data in spawnAreas)
            {
                GameObject instance = Instantiate(
                    npcSpawnerPrefab,
                    new Vector3(data.Position.x, data.Position.y, data.Position.z),
                    Quaternion.identity
                );
                NPCSpawns npcSpawnData = instance.GetComponent<NPCSpawns>();
                if (npcSpawnData == null)
                {
                    logger.Log(
                        "NPCSpawns component missing on NPC spawner instance; destroying instance and skipping.",
                        this
                    );
                    Destroy(instance);
                    continue;
                }
                npcSpawnData.ConfigureFromSpawnData(data, null); //TODO: dialog is missing, add later when ready
                instance.name = $"NPCSpawner";
                NetworkServer.Spawn(instance);

                // when dialog data is available, check if it should be called spawn function here after configuration
            }
            logger.Log("Finished setting up NPC spawners!", this);
        }
    }
}
