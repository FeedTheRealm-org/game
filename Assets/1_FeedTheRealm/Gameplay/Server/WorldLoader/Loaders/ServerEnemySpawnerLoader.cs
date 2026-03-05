using System.Collections.Generic;
using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTRShared.Runtime.Models;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Server.WorldLoader.Loaders
{
    public class EnemySpawnerLoaderController : MonoBehaviour, ILoader
    {
        [SerializeField]
        private GameObject spawnerPrefab;

        public async UniTask Load(WorldData worldData)
        {
            var spawnAreas = worldData.enemySpawnAreas;
            foreach (EnemySpawnerData data in spawnAreas)
            {
                GameObject instance = Instantiate(
                    spawnerPrefab,
                    new Vector3(data.Position.x, data.Position.y, data.Position.z),
                    Quaternion.identity
                );
                EnemySpawn enemySpawnData = instance.GetComponent<EnemySpawn>();
                enemySpawnData.SetupSpawner(data);
                instance.name = "EnemySpawner";
            }
        }
    }
}
