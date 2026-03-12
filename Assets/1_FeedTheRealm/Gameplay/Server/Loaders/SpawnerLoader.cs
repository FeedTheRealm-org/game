using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Loaders
{
    public abstract class BaseSpawnerLoader<TSpawnerData, TSpawnerComponent>
        : MonoBehaviour,
            ILoader
        where TSpawnerComponent : Component
    {
        [SerializeField]
        private GameObject spawnerPrefab;

        public UniTask Load(WorldData worldData)
        {
            foreach (TSpawnerData data in GetSpawnAreas(worldData))
            {
                TSpawnerComponent spawner = InstantiateSpawner(data);
                InitializeSpawner(spawner, data);
                spawner.gameObject.name = GetSpawnerName(data);
            }

            return UniTask.CompletedTask;
        }

        protected virtual TSpawnerComponent InstantiateSpawner(TSpawnerData data)
        {
            GameObject instance = Instantiate(
                spawnerPrefab,
                GetSpawnPosition(data),
                Quaternion.identity
            );
            TSpawnerComponent spawner = instance.GetComponent<TSpawnerComponent>();

            if (spawner == null)
            {
                throw new InvalidOperationException(
                    $"Spawner prefab '{spawnerPrefab.name}' must include a {typeof(TSpawnerComponent).Name} component."
                );
            }

            return spawner;
        }

        protected abstract IEnumerable<TSpawnerData> GetSpawnAreas(WorldData worldData);

        protected abstract Vector3 GetSpawnPosition(TSpawnerData data);

        protected abstract void InitializeSpawner(TSpawnerComponent spawner, TSpawnerData data);

        protected abstract string GetSpawnerName(TSpawnerData data);
    }
}
