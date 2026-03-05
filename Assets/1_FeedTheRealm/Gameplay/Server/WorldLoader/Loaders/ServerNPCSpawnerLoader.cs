using System.Collections.Generic;
using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTRShared.Runtime.Models;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Server.WorldLoader.Loaders
{
    public class NPCSpawnerLoaderController : MonoBehaviour, ILoader
    {
        [SerializeField]
        private GameObject spawnerPrefab;

        public async UniTask Load(WorldData worldData)
        {
            var spawnAreas = worldData.npcSpawnAreas;
            if (spawnAreas == null || spawnAreas.Count == 0)
                return;

            foreach (NPCSpawnerData data in spawnAreas)
            {
                GameObject instance = Object.Instantiate(
                    spawnerPrefab,
                    new Vector3(data.Position.x, data.Position.y, data.Position.z),
                    Quaternion.identity
                );
                NPCSpawns npcSpawnData = instance.GetComponent<NPCSpawns>();
                npcSpawnData.ConfigureFromSpawnData(data, null); // TODO: dialog is missing, add later when ready
                instance.name = "NPCSpawner";
            }
        }
    }
}
