using System.Collections.Generic;
using System.Threading.Tasks;
using API;
using Mirror;
using Models;
using UnityEngine;

namespace Core.Systems.Worlds.Loader
{
    public class NPCSpawnerLoaderController : MonoBehaviour, IServerLoader
    {
        [Header("Logger")]
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private GameObject npcSpawnerPrefab;

        public async Task LoadServer(WorldData worldData, string accessToken)
        {
            logger.Log(
                "[NPCSpawnerLoader][Server] Setting up NPC spawners | Amount "
                    + worldData.npcSpawnAreas.Count,
                this
            );
            foreach (NPCSpawnerData data in worldData.npcSpawnAreas)
            {
                GameObject instance = Instantiate(
                    npcSpawnerPrefab,
                    new Vector3(data.Position.x, data.Position.y, data.Position.z),
                    Quaternion.identity
                );
                NPCSpawns npcSpawnData = instance.GetComponent<NPCSpawns>();
                npcSpawnData.ConfigureFromSpawnData(data, null); //TODO: dialog is missing, add later when ready
                instance.name = $"NPCSpawner";
                NetworkServer.Spawn(instance);

                // when dialog data is available, check if it should be called spawn function here after configuration
            }
            logger.Log("[NPCSpawnerLoader][Server] Finished setting up NPC spawners!", this);
        }
    }
}
