using System.Collections.Generic;
using System.Threading.Tasks;
using API;
using Mirror;
using Models;
using UnityEngine;

namespace Core.Systems.Worlds.Loader
{
    public class EnemySpawnerLoaderController : MonoBehaviour, IServerLoader
    {
        [Header("Logger")]
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private GameObject enemySpawnerPrefab;

        public async Task LoadServer(WorldData worldData, string accessToken)
        {
            if (worldData == null)
            {
                logger.Log("No world data provided; skipping enemy spawner setup.", this);
                return;
            }
            var spawnAreas = worldData.enemySpawnAreas;
            if (spawnAreas == null || spawnAreas.Count == 0)
            {
                logger.Log("No enemy spawn areas found; skipping enemy spawner setup.", this);
                return;
            }
            if (enemySpawnerPrefab == null)
            {
                logger.Log(
                    "enemySpawnerPrefab is not assigned; cannot create enemy spawners.",
                    this
                );
                return;
            }
            logger.Log("Setting up enemy spawners | Amount " + spawnAreas.Count, this);
            foreach (EnemySpawnerData data in spawnAreas)
            {
                GameObject instance = Instantiate(
                    enemySpawnerPrefab,
                    new Vector3(data.Position.x, data.Position.y, data.Position.z),
                    Quaternion.identity
                );
                EnemySpawn enemySpawnData = instance.GetComponent<EnemySpawn>();
                if (enemySpawnData == null)
                {
                    logger.Log(
                        "EnemySpawn component missing on enemy spawner instance; destroying instance and skipping.",
                        this
                    );
                    Destroy(instance);
                    continue;
                }
                enemySpawnData.SetupSpawner(data);
                instance.name = $"EnemySpawner";
                NetworkServer.Spawn(instance);
            }
            logger.Log("Finished setting up enemy spawners!", this);
        }
    }
}
