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
            logger.Log(
                "[EnemySpawnerLoader][Server] Setting up enemy spawners | Amount "
                    + worldData.enemySpawnAreas.Count,
                this
            );
            foreach (EnemySpawnerData data in worldData.enemySpawnAreas)
            {
                GameObject instance = Instantiate(
                    enemySpawnerPrefab,
                    new Vector3(data.Position.x, data.Position.y, data.Position.z),
                    Quaternion.identity
                );
                EnemySpawn enemySpawnData = instance.GetComponent<EnemySpawn>();
                enemySpawnData.SetupSpawner(data);
                instance.name = $"EnemySpawner";
                NetworkServer.Spawn(instance);
            }
            logger.Log("[EnemySpawnerLoader][Server] Finished setting up enemy spawners!", this);
        }
    }
}
