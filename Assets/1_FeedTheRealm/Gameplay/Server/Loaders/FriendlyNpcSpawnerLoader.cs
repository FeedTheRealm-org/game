using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTR.Core.Server;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Loaders
{
    public class FriendlyNpcSpawnerLoader : ILoader
    {
        private readonly GameObject spawnerPrefab;

        public FriendlyNpcSpawnerLoader(ServerPrefabProvider prefabProvider)
        {
            spawnerPrefab = prefabProvider.FriendlyNpcSpawnerComponent;
        }

        public async UniTask Load(WorldData worldData)
        {
            var spawnAreas = worldData.npcSpawnAreas;
            foreach (NPCSpawnerData data in spawnAreas)
            {
                GameObject instance = Object.Instantiate(
                    spawnerPrefab,
                    new Vector3(data.Position.x, data.Position.y, data.Position.z),
                    Quaternion.identity
                );
                NPCSpawns npcSpawnData = instance.GetComponent<NPCSpawns>();
                npcSpawnData.Initialize(data, null); // TODO: dialog is missing, add later when ready
                instance.name = "NPCSpawner";
            }
        }
    }
}
