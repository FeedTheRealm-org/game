using Cysharp.Threading.Tasks;
using FTR.Core.Common.Loaders;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Loaders
{
    public class NPCSpawnerLoader : MonoBehaviour, ILoader
    {
        [SerializeField]
        private GameObject spawnerPrefab;

        public async UniTask Load(WorldData worldData)
        {
            var spawnAreas = worldData.npcSpawnAreas;
            foreach (NPCSpawnerData data in spawnAreas)
            {
                GameObject instance = Instantiate(
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
