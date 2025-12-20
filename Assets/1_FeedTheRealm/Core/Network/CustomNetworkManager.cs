using UnityEngine;
using Mirror;

/// <summary>
/// Custom NetworkManager for Feed The Realm.
/// Overrides player spawning to use PlayerSpawnManager's spawn points.
/// </summary>
public class CustomNetworkManager : NetworkManager
{
    [Header("FTR Custom Settings")]
    [SerializeField] private PlayerSpawnManager spawnManager;
    [SerializeField] private Logging.Logger logger;

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Auto-find PlayerSpawnManager if not assigned
        if (spawnManager == null)
        {
            spawnManager = FindFirstObjectByType<PlayerSpawnManager>();
        }

        if (spawnManager == null)
        {
            logger?.Log("[CustomNetworkManager] WARNING: PlayerSpawnManager not found! Players will spawn at default position.", this, Logging.LogType.Warning);
        }
        else
        {
            logger?.Log("[CustomNetworkManager] PlayerSpawnManager found and ready.", this);
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // Get spawn position from PlayerSpawnManager
        Transform startPos = GetStartPosition();

        // Instantiate player at spawn position
        GameObject player = startPos != null
            ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
            : Instantiate(playerPrefab);

        // Spawn the player for this connection
        NetworkServer.AddPlayerForConnection(conn, player);

        logger?.Log($"[CustomNetworkManager] Player spawned for connection {conn.connectionId} at position {player.transform.position}", this);
    }

    public override Transform GetStartPosition()
    {
        // Try to find PlayerSpawnManager if not already set (handles cross-scene references)
        if (spawnManager == null)
        {
            spawnManager = FindFirstObjectByType<PlayerSpawnManager>();
            if (spawnManager != null)
            {
                logger?.Log("[CustomNetworkManager] PlayerSpawnManager found dynamically in scene", this);
            }
        }

        // Use PlayerSpawnManager if available
        if (spawnManager != null && spawnManager.spawnPoints != null && spawnManager.spawnPoints.Length > 0)
        {
            // Use connectionId to determine spawn point index
            int connectionCount = NetworkServer.connections.Count;
            int spawnIndex = (connectionCount - 1) % spawnManager.spawnPoints.Length;

            Transform spawnPoint = spawnManager.spawnPoints[spawnIndex];
            logger?.Log($"[CustomNetworkManager] Using spawn point {spawnIndex}: {spawnPoint.position}", this);
            return spawnPoint;
        }

        // Fallback to base implementation (uses startPositions list or NetworkManager position)
        logger?.Log("[CustomNetworkManager] No PlayerSpawnManager available, using default spawn", this, Logging.LogType.Warning);
        return base.GetStartPosition();
    }
}
