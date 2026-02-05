using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages spawn points for players in multiplayer.
/// Works with NewNetworkManager to spawn players at correct positions.
/// </summary>
public class PlayerSpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;

    [Header("Ground Reference")]
    [SerializeField]
    private Transform groundReference;

    [SerializeField]
    private bool adjustToGroundHeight = true;

    [SerializeField]
    private float heightOffset = 0f;

    [SerializeField]
    private Logging.Logger logger;

    /// <summary>
    /// Sets player spawn points from WorldData (playerSpawnAreas) dynamically. If no data,
    /// returns without configuring spawn points (NetworkManager will use default spawning).
    /// Called by NewNetworkManager.OnStartServer() - server-only context is guaranteed by caller.
    /// </summary>
    public void ConfigureSpawnPointsFromWorldData(Models.WorldData worldData)
    {
        if (
            worldData == null
            || worldData.playerSpawnAreas == null
            || worldData.playerSpawnAreas.Count == 0
        )
        {
            logger.Log(
                "[PlayerSpawnManager] WorldData has no playerSpawnAreas; NetworkManager will use default spawn.",
                this
            );
            return;
        }

        var newSpawnPoints = new List<Transform>();

        for (int i = 0; i < worldData.playerSpawnAreas.Count; i++)
        {
            var area = worldData.playerSpawnAreas[i];
            Vector3 position = area.Position;

            if (adjustToGroundHeight && groundReference != null)
            {
                position.y = groundReference.position.y + heightOffset;
            }

            var go = new GameObject($"WorldPlayerSpawn_{i}");
            go.transform.SetParent(transform, false);
            go.transform.position = position;

            newSpawnPoints.Add(go.transform);
            logger.Log($"[PlayerSpawnManager] Created world spawn point {i} at {position}.", this);
        }

        spawnPoints = newSpawnPoints.ToArray();
        logger.Log(
            $"[PlayerSpawnManager] Configured {spawnPoints.Length} spawn points from world data.",
            this
        );
    }

    /// <summary>
    /// Gets spawn position by player index.
    /// Called by CustomNetworkManager.
    /// </summary>
    public Vector3 GetSpawnPositionByIndex(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnIndex = index % spawnPoints.Length;
            Vector3 position = spawnPoints[spawnIndex].position;

            if (adjustToGroundHeight && groundReference != null)
            {
                position.y = groundReference.position.y + heightOffset;
                logger.Log(
                    $"[PlayerSpawnManager] Height adjusted to ground. Original Y: {spawnPoints[spawnIndex].position.y}, Adjusted Y: {position.y}",
                    this
                );
            }

            return position;
        }

        logger.Log(
            "[PlayerSpawnManager] No spawn points, using Vector3.zero",
            this,
            Logging.LogType.Warning
        );
        return Vector3.zero;
    }

    /// <summary>
    /// Gets spawn rotation by player index.
    /// Called by CustomNetworkManager.
    /// </summary>
    public Quaternion GetSpawnRotationByIndex(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnIndex = index % spawnPoints.Length;
            return spawnPoints[spawnIndex].rotation;
        }

        return Quaternion.identity;
    }
}
