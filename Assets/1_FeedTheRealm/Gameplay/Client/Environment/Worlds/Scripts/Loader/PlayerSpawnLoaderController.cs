using System.Collections.Generic;
using System.Threading.Tasks;
using API;
using Mirror;
using Models;
using UnityEngine;

namespace FTR.Gameplay.Client.Environment.Worlds.Loader
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
            if (worldData == null)
            {
                logger.Log("No world data provided; skipping player spawn point setup.", this);
                return;
            }
            var spawnAreas = worldData.playerSpawnAreas;
            if (spawnAreas == null || spawnAreas.Count == 0)
            {
                logger.Log(
                    "No player spawn areas found; NetworkManager will use default spawn.",
                    this
                );
                return;
            }
            if (playerSpawnPointPrefab == null)
            {
                logger.Log(
                    "playerSpawnPointPrefab is not assigned; cannot create player spawn points.",
                    this
                );
                return;
            }
            logger.Log("Setting up player spawn points | Amount " + spawnAreas.Count, this);

            foreach (PlayerSpawnerData area in spawnAreas)
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
        }
    }
}
