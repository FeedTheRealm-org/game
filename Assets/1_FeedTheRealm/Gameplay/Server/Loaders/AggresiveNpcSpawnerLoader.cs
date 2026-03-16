using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Core.Server;
using FTR.Gameplay.Server.Environment.Spawns;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Loaders
{
    public class AggressiveNpcSpawnerLoader : ILoader
    {
        private readonly GameObject spawnerPrefab;

        public AggressiveNpcSpawnerLoader(ServerPrefabProvider prefabProvider)
        {
            spawnerPrefab = prefabProvider.AggresiveNpcSpawnerComponent;
        }

        public async UniTask Load(WorldData worldData)
        {
            var spawnAreas = worldData.enemySpawnAreas;
            foreach (EnemySpawnerData data in spawnAreas)
            {
                GameObject instance = Object.Instantiate(
                    spawnerPrefab,
                    new Vector3(data.Position.x, data.Position.y, data.Position.z),
                    Quaternion.identity
                );
                EnemySpawn enemySpawnData = instance.GetComponent<EnemySpawn>();
                enemySpawnData.Initialize(data);
                instance.name = "EnemySpawner";
            }
        }
    }
}
