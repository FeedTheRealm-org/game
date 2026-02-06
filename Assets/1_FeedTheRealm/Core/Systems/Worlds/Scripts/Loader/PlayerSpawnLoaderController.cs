using System.Collections.Generic;
using System.Threading.Tasks;
using API;
using Mirror;
using Models;
using UnityEngine;

namespace Core.Systems.Worlds.Loader
{
    public class PlayerSpawnLoaderController : MonoBehaviour, IServerLoader
    {
        [Header("Logger")]
        [SerializeField]
        private Logging.Logger logger;

        [Header("Prefab")]
        [SerializeField]
        private GameObject playerSpawnPointPrefab;

        [Header("Ground Reference")]
        [SerializeField]
        private Transform groundReference;

        public async Task LoadServer(WorldData worldData, string accessToken)
        {
            logger.Log(
                "[PlayerSpawnLoader][Server] Setting up player spawn points | Amount "
                    + worldData.playerSpawnAreas.Count,
                this
            );

            if (
                worldData == null
                || worldData.playerSpawnAreas == null
                || worldData.playerSpawnAreas.Count == 0
            )
            {
                logger.Log(
                    "[PlayerSpawnLoader][Server] WorldData has no playerSpawnAreas; NetworkManager will use default spawn.",
                    this
                );
                return;
            }

            foreach (PlayerSpawnerData area in worldData.playerSpawnAreas)
            {
                GameObject spawnInstance = Instantiate(
                    playerSpawnPointPrefab,
                    area.Position,
                    Quaternion.identity
                );

                // Configure the spawn point with data from world
                PlayerSpawnPoint spawnComponent = spawnInstance.GetComponent<PlayerSpawnPoint>();
                if (spawnComponent != null)
                {
                    spawnComponent.ConfigureFromSpawnData(area, groundReference);
                }
                else
                {
                    logger.Log(
                        "[PlayerSpawnLoader][Server] Player spawn prefab missing PlayerSpawnPoint component!",
                        this,
                        Logging.LogType.Error
                    );
                }

                spawnInstance.SetActive(true);
            }

            logger.Log(
                $"[PlayerSpawnLoader][Server] Finished setting up {worldData.playerSpawnAreas.Count} player spawn points!",
                this
            );
        }
    }
}
