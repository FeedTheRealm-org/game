using System.Collections.Generic;
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
            var spawnAreas = zoneData?.enemySpawnAreas;
            if (spawnAreas == null || spawnAreas.Count == 0)
                return;

            var enemyById = BuildEnemyLookup(creatablesData?.enemies);

            foreach (EnemySpawnerData data in spawnAreas)
            {
                enemyById.TryGetValue(data.EnemyId, out var enemyData);

                if (string.IsNullOrEmpty(data.EnemyId))
                {
                    Debug.LogWarning(
                        "[AggressiveNpcSpawnerLoader] EnemySpawnerData has no EnemyId."
                    );
                }
                else if (enemyData == null)
                {
                    Debug.LogWarning(
                        $"[AggressiveNpcSpawnerLoader] No EnemyData found for EnemyId '{data.EnemyId}'. Spawner will fallback to EnemyId."
                    );
                }

                GameObject instance = resolver.Instantiate(
                    spawnerPrefab,
                    new Vector3(data.Position.x, data.Position.y, data.Position.z),
                    Quaternion.identity
                );
                instance.transform.localScale = Vector3.one;
                EnemySpawn enemySpawnData = instance.GetComponent<EnemySpawn>();
                enemySpawnData.Initialize(data, enemyData);
                instance.name = string.IsNullOrEmpty(data.EnemyId)
                    ? "EnemySpawner"
                    : $"EnemySpawner_{data.EnemyId}";
            }
        }

        private static Dictionary<string, EnemyData> BuildEnemyLookup(List<EnemyData> enemies)
        {
            var dict = new Dictionary<string, EnemyData>();
            if (enemies == null)
                return dict;

            foreach (var enemy in enemies)
            {
                if (enemy == null || string.IsNullOrEmpty(enemy.id))
                    continue;
                if (dict.ContainsKey(enemy.id))
                {
                    Debug.LogWarning(
                        $"[AggressiveNpcSpawnerLoader] Duplicate EnemyData id '{enemy.id}', skipping."
                    );
                    continue;
                }

                dict[enemy.id] = enemy;
            }

            return dict;
        }
    }
}
