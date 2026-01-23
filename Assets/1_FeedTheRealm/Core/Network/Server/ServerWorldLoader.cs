using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using API;
using Mirror;
using Models;
using Session;
using UnityEngine;
using Worlds;

/// <summary>
/// Loads world configuration on the dedicated server from a
/// command line parameter (-world ID) and applies:
/// - Enemy spawn areas (EnemySpawnAreaData)
/// - Player spawn points (PlayerSpawnAreaData) for PlayerSpawnManager
///
/// This component should be in the server game scene
/// (the same scene where NetworkManager / PlayerSpawnManager are).
/// </summary>
public class ServerWorldLoader : NetworkBehaviour
{
    [Header("World Config Services")]
    [SerializeField]
    private WorldService worldService;

    [SerializeField]
    private Session.Session session;

    [SerializeField]
    private WorldHandler worldHandler;

    [SerializeField]
    private GameObject enemySpawnPrefab;

    [SerializeField]
    private GameObject npcSpawnerPrefab;

    [SerializeField]
    private PlayerSpawnManager playerSpawnManager;

    [Header("Logging")]
    [SerializeField]
    private Logging.Logger logger;

    [Header("Debug")]
    [SerializeField]
    private bool logWorldDataJson = false;

    private const string WorldArgShort = "-world";
    private const string WorldArgLong = "--world";

    public override void OnStartServer()
    {
        base.OnStartServer();
        _ = RunServerWorldLoadAsync();
    }

    private async Task RunServerWorldLoadAsync()
    {
        try
        {
            string worldId = GetWorldIdFromArgs();
            if (string.IsNullOrWhiteSpace(worldId))
            {
                logger?.Log(
                    "[ServerWorldLoader] No -world argument found; keeping default scene configuration.",
                    this
                );
                return;
            }

            if (worldService == null || session == null)
            {
                logger?.Log(
                    "[ServerWorldLoader] WorldService or Session not assigned; cannot load world from API.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            var envToken = System.Environment.GetEnvironmentVariable("FTR_SERVER_API_TOKEN");
            logger?.Log(
                $"[ServerWorldLoader] Env token prefix: {envToken?.Substring(0, 8)}...",
                this
            );

            if (string.IsNullOrWhiteSpace(envToken))
            {
                logger?.Log(
                    "[ServerWorldLoader] No API token in environment variable FTR_SERVER_API_TOKEN; cannot authenticate against backend.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }
            else
            {
                session.SetAPIToken(envToken);
                logger?.Log(
                    "[ServerWorldLoader] Using API token from environment variable FTR_SERVER_API_TOKEN.",
                    this
                );
            }

            await LoadWorldFromService(worldId);
        }
        catch (System.Exception ex)
        {
            logger?.Log(
                $"[ServerWorldLoader] Exception in OnStartServer: {ex.Message}",
                this,
                Logging.LogType.Error
            );
        }
    }

    /// <summary>
    /// Reads the world ID from command line arguments (-world ID).
    /// </summary>
    private string GetWorldIdFromArgs()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == WorldArgShort || args[i] == WorldArgLong)
            {
                string worldId = args[i + 1];
                logger?.Log($"[ServerWorldLoader] World ID argument found: '{worldId}'", this);
                return worldId;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Calls WorldService to get world data by ID
    /// and applies its WorldData to enemy and player spawns.
    /// </summary>
    private async Task LoadWorldFromService(string worldId)
    {
        logger?.Log(
            $"[ServerWorldLoader] Loading world configuration for '{worldId}' from API...",
            this
        );

        var (worldData, errorMessage, code) = await worldService.GetWorldData(
            worldId,
            session.APIToken
        );

        if (!string.IsNullOrEmpty(errorMessage))
        {
            logger?.Log(
                $"[ServerWorldLoader] Failed to load world '{worldId}': {errorMessage}"
                    + $" (code: {code})",
                this,
                Logging.LogType.Error
            );
            return;
        }

        if (worldData == null)
        {
            logger?.Log(
                $"[ServerWorldLoader] World '{worldId}' loaded but has no data.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        if (worldHandler != null)
        {
            worldHandler.selectedWorld = worldData;
        }

        // Expose world items/enemies to gameplay systems (loot, inventory, etc.)
        Worlds.WorldItemsRegistry.RegisterWorldData(worldData);
        if (logWorldDataJson && worldData != null)
        {
            try
            {
                string json = JsonUtility.ToJson(worldData, true);
                logger?.Log(
                    $"[ServerWorldLoader] World data JSON for '{worldData.worldName}':\n{json}",
                    this
                );
            }
            catch (System.Exception ex)
            {
                logger?.Log(
                    $"[ServerWorldLoader] Failed to serialize world data for logging: {ex.Message}",
                    this,
                    Logging.LogType.Warning
                );
            }
        }

        // Configure enemy spawns on server
        if (enemySpawnPrefab != null && worldData.enemySpawnAreas != null)
        {
            foreach (EnemySpawnerData area in worldData.enemySpawnAreas)
            {
                GameObject spawnInstance = Instantiate(
                    enemySpawnPrefab,
                    area.Position,
                    Quaternion.identity
                );

                // Configure the spawn with data from world
                EnemySpawn spawnComponent = spawnInstance.GetComponent<EnemySpawn>();
                if (spawnComponent != null)
                {
                    spawnComponent.ConfigureFromSpawnData(area);
                }
                else
                {
                    logger?.Log(
                        "[ServerWorldLoader] Enemy spawn prefab missing EnemySpawn component!",
                        this,
                        Logging.LogType.Error
                    );
                }

                spawnInstance.SetActive(true);
            }
            logger?.Log(
                $"[ServerWorldLoader] Placed {worldData.enemySpawnAreas.Count} enemy spawn areas from world data (world-space).",
                this
            );
        }
        else
        {
            logger?.Log(
                "[ServerWorldLoader] Enemy spawn configuration skipped (missing WorldController, prefab or data).",
                this,
                Logging.LogType.Warning
            );
        }

        if (npcSpawnerPrefab != null && worldData.npcSpawnAreas != null)
        {
            foreach (NPCSpawnerData area in worldData.npcSpawnAreas)
            {
                GameObject spawnInstance = Instantiate(
                    npcSpawnerPrefab,
                    area.Position,
                    Quaternion.identity
                );

                // Configure the spawn with data from world
                NPCSpawns spawnComponent = spawnInstance.GetComponent<NPCSpawns>();
                if (spawnComponent != null)
                {
                    spawnComponent.ConfigureFromSpawnData(area, worldData.dialogs[0]);
                }
                else
                {
                    logger?.Log(
                        "[ServerWorldLoader] NPC spawn prefab missing NPCSpawns component!",
                        this,
                        Logging.LogType.Error
                    );
                }

                spawnInstance.SetActive(true);
            }
            logger?.Log(
                $"[ServerWorldLoader] Placed {worldData.npcSpawnAreas.Count} NPC spawn areas from world data (world-space).",
                this
            );
        }
        else
        {
            logger?.Log(
                "[ServerWorldLoader] NPC spawn configuration skipped (missing WorldController, prefab or data).",
                this,
                Logging.LogType.Warning
            );
        }

        // set player spawns using PlayerSpawnManager on server
        if (playerSpawnManager != null)
        {
            playerSpawnManager.ConfigureSpawnPointsFromWorldData(worldData);
        }
        else
        {
            logger?.Log(
                "[ServerWorldLoader] PlayerSpawnManager not assigned; cannot configure player spawns from world.",
                this,
                Logging.LogType.Warning
            );
        }

        logger?.Log(
            $"[ServerWorldLoader] World '{worldId}' configuration applied on server.",
            this
        );
    }
}
