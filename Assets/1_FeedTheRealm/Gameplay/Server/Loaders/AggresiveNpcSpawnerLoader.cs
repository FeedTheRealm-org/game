using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Core.Server;
using FTR.Gameplay.Server.Environment.Spawns;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Loaders
{
    public class AggressiveNpcSpawnerLoader : ILoader
    {
        private readonly GameObject spawnerPrefab;
        private readonly IObjectResolver resolver;

        public AggressiveNpcSpawnerLoader(
            ServerPrefabProvider prefabProvider,
            IObjectResolver resolver
        )
        {
            spawnerPrefab = prefabProvider.AggresiveNpcSpawnerComponent;
            this.resolver = resolver;
        }

        public async UniTask Load(string worldId, ZoneData zoneData, CreatablesData creatablesData)
        {
            var spawnAreas = zoneData.enemySpawnAreas;
            foreach (EnemySpawnerData data in spawnAreas)
            {
                GameObject instance = resolver.Instantiate(
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
