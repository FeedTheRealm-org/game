using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using API;
using Models;
using Session;
using World;
using Worlds;

/// <summary>
/// Loads world configuration on the dedicated server from a
/// command line parameter (-world NAME) and applies:
/// - Enemy spawn areas (EnemySpawnAreaData)
/// - Player spawn points (PlayerSpawnAreaData) for PlayerSpawnManager
///
/// This component should be in the server game scene
/// (the same scene where NetworkManager / PlayerSpawnManager are).
/// </summary>
public class ServerWorldLoader : NetworkBehaviour {
    [Header("World Config Services")]
    [SerializeField] private WorldService worldService;
    [SerializeField] private Session.Session session;
    [SerializeField] private WorldHandler worldHandler;

    [Header("Scene References")] 
    [SerializeField] private WorldController worldController;
    [SerializeField] private GameObject enemySpawnPrefab;
    [SerializeField] private PlayerSpawnManager playerSpawnManager;

    [Header("Logging")]
    [SerializeField] private Logging.Logger logger;

    [Header("Debug")]
    [SerializeField] private bool logWorldDataJson = false;

    private const string WorldArgShort = "-world";
    private const string WorldArgLong = "--world";

    public override void OnStartServer() {
        base.OnStartServer();

        string worldName = GetWorldNameFromArgs();
        if (string.IsNullOrWhiteSpace(worldName)) {
            logger?.Log("[ServerWorldLoader] No -world argument found; keeping default scene configuration.", this);
            return;
        }

        if (worldService == null || session == null) {
            logger?.Log("[ServerWorldLoader] WorldService or Session not assigned; cannot load world from API.", this, Logging.LogType.Error);
            return;
        }

        var envToken = System.Environment.GetEnvironmentVariable("FTR_SERVER_API_TOKEN");
        logger?.Log($"[ServerWorldLoader] Env token prefix: {envToken?.Substring(0, 8)}...", this);

        if (string.IsNullOrWhiteSpace(envToken)) {
            logger?.Log("[ServerWorldLoader] No API token in environment variable FTR_SERVER_API_TOKEN; cannot authenticate against backend.", this, Logging.LogType.Error);
            return;
        }
        else {
            session.SetAPIToken(envToken);
            logger?.Log("[ServerWorldLoader] Using API token from environment variable FTR_SERVER_API_TOKEN.", this);
        }

        StartCoroutine(LoadWorldFromService(worldName));
    }

    /// <summary>
    /// Reads the world name from command line arguments (-world NAME).
    /// </summary>
    private string GetWorldNameFromArgs() {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) {
            if (args[i] == WorldArgShort || args[i] == WorldArgLong) {
                string worldName = args[i + 1];
                logger?.Log($"[ServerWorldLoader] World argument found: '{worldName}'", this);
                return worldName;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Calls WorldService to get world metadata by name (using filter)
    /// and applies its WorldData to enemy and player spawns.
    /// </summary>
    private IEnumerator LoadWorldFromService(string worldName) {
        logger?.Log($"[ServerWorldLoader] Loading world configuration for '{worldName}' from API...", this);

        WorldMetadata selected = null;
        string errorMessage = null;

        // Use GetWorldPage with filter by name, limit 1
        yield return worldService.GetWorldPage(0, 1, worldName, session.APIToken,
            (amount, worlds, error) => {
                if (!string.IsNullOrEmpty(error)) {
                    errorMessage = error;
                    return;
                }

                if (worlds == null || worlds.Count == 0) {
                    errorMessage = $"World '{worldName}' not found";
                    return;
                }

                selected = worlds[0];
            });

        if (!string.IsNullOrEmpty(errorMessage)) {
            logger?.Log($"[ServerWorldLoader] Failed to load world '{worldName}': {errorMessage}", this, Logging.LogType.Error);
            yield break;
        }

        if (selected == null || selected.data == null) {
            logger?.Log($"[ServerWorldLoader] World '{worldName}' loaded but has no data.", this, Logging.LogType.Error);
            yield break;
        }

        // Save the selected world in the shared WorldHandler, if it exists
        if (worldHandler != null) {
            worldHandler.SetSelectedWorld(selected);
        }

        WorldData data = selected.data;

        if (logWorldDataJson && data != null) {
            try {
                string json = JsonUtility.ToJson(data, true);
                logger?.Log($"[ServerWorldLoader] World data JSON for '{selected.name}':\n{json}", this);
            } catch (System.Exception ex) {
                logger?.Log($"[ServerWorldLoader] Failed to serialize world data for logging: {ex.Message}", this, Logging.LogType.Warning);
            }
        }

        // Configure enemy spawns on server
        if (worldController != null && enemySpawnPrefab != null && data.enemySpawnAreas != null) {
            foreach (EnemySpawnAreaData area in data.enemySpawnAreas) {
                Vector3Int gridPos = Vector3Int.FloorToInt(area.Position);
                worldController.PlaceEnemySpawnAreaAt(gridPos, enemySpawnPrefab);
            }
            logger?.Log($"[ServerWorldLoader] Placed {data.enemySpawnAreas.Count} enemy spawn areas from world data.", this);
        } else {
            logger?.Log("[ServerWorldLoader] Enemy spawn configuration skipped (missing WorldController, prefab or data).", this, Logging.LogType.Warning);
        }

        // set player spawns using PlayerSpawnManager on server
        if (playerSpawnManager != null) {
            playerSpawnManager.ConfigureSpawnPointsFromWorldData(data);
        } else {
            logger?.Log("[ServerWorldLoader] PlayerSpawnManager not assigned; cannot configure player spawns from world.", this, Logging.LogType.Warning);
        }

        logger?.Log($"[ServerWorldLoader] World '{worldName}' configuration applied on server.", this);
    }
}
