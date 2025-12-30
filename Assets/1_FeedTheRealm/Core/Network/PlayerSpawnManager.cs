using UnityEngine;
using Mirror;
using System.Collections.Generic;

/// <summary>
/// Manages spawn points for players in multiplayer.
/// Works with CustomNetworkManager to spawn players at correct positions.
/// </summary>
public class PlayerSpawnManager : NetworkBehaviour {
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;

    [Header("Auto-detect Spawn Points")]
    [SerializeField] private bool autoDetectSpawnPoints = true;
    [SerializeField] private string spawnPointTag = "SpawnPoint";

    [Header("Ground Reference")]
    [SerializeField] private Transform groundReference;
    [SerializeField] private bool adjustToGroundHeight = true;
    [SerializeField] private float heightOffset = 0f;

    [SerializeField] private Logging.Logger logger;

    private void Awake() {
        if (autoDetectSpawnPoints) {
            DetectSpawnPoints();
        }
    }

    private void DetectSpawnPoints() {
        List<Transform> detectedSpawnPoints = new List<Transform>();

        foreach (Transform child in transform) {
            detectedSpawnPoints.Add(child);
        }

        GameObject[] taggedSpawnPoints = GameObject.FindGameObjectsWithTag(spawnPointTag);
        foreach (GameObject spawnObj in taggedSpawnPoints) {
            if (!detectedSpawnPoints.Contains(spawnObj.transform)) {
                detectedSpawnPoints.Add(spawnObj.transform);
            }
        }

        if (detectedSpawnPoints.Count > 0) {
            spawnPoints = detectedSpawnPoints.ToArray();
            logger.Log($"[PlayerSpawnManager] Detected {spawnPoints.Length} spawn points automatically", this);
        } else {
            logger.Log("[PlayerSpawnManager] No spawn points detected. Using default position.", this, Logging.LogType.Warning);
        }
    }

    /// <summary>
    /// Sets player spawn points from WorldData (playerSpawnAreas) dynamically. If no data, keeps
    /// the current configuration.
    /// </summary>
    [Server]
    public void ConfigureSpawnPointsFromWorldData(Models.WorldData worldData) {
        if (worldData == null || worldData.playerSpawnAreas == null || worldData.playerSpawnAreas.Count == 0) {
            logger.Log("[PlayerSpawnManager] WorldData has no playerSpawnAreas; keeping existing spawnPoints.", this, Logging.LogType.Warning);
            return;
        }

        var newSpawnPoints = new List<Transform>();

        for (int i = 0; i < worldData.playerSpawnAreas.Count; i++) {
            var area = worldData.playerSpawnAreas[i];
            Vector3 position = area.Position;

            if (adjustToGroundHeight && groundReference != null) {
                position.y = groundReference.position.y + heightOffset;
            }

            var go = new GameObject($"WorldPlayerSpawn_{i}");
            go.transform.SetParent(transform, false);
            go.transform.position = position;

            newSpawnPoints.Add(go.transform);
            logger.Log($"[PlayerSpawnManager] Created world spawn point {i} at {position}.", this);
        }

        spawnPoints = newSpawnPoints.ToArray();
        logger.Log($"[PlayerSpawnManager] Configured {spawnPoints.Length} spawn points from world data.", this);
    }

    public override void OnStartServer() {
        if (spawnPoints == null || spawnPoints.Length == 0) {
            logger.Log("[PlayerSpawnManager] No spawn points configured!", this, Logging.LogType.Error);
        } else {
            logger.Log($"[PlayerSpawnManager] Spawn Manager activated with {spawnPoints.Length} spawn points", this);

            for (int i = 0; i < spawnPoints.Length; i++) {
                logger.Log($"[PlayerSpawnManager] Spawn Point {i}: Position = {spawnPoints[i].position}, Rotation = {spawnPoints[i].rotation.eulerAngles}", this);
            }
        }
    }

    /// <summary>
    /// Gets spawn position by player index.
    /// Called by CustomNetworkManager.
    /// </summary>
    public Vector3 GetSpawnPositionByIndex(int index) {
        if (spawnPoints != null && spawnPoints.Length > 0) {
            int spawnIndex = index % spawnPoints.Length;
            Vector3 position = spawnPoints[spawnIndex].position;

            if (adjustToGroundHeight && groundReference != null) {
                position.y = groundReference.position.y + heightOffset;
                logger.Log($"[PlayerSpawnManager] Height adjusted to ground. Original Y: {spawnPoints[spawnIndex].position.y}, Adjusted Y: {position.y}", this);
            }

            return position;
        }

        logger.Log("[PlayerSpawnManager] No spawn points, using Vector3.zero", this, Logging.LogType.Warning);
        return Vector3.zero;
    }

    /// <summary>
    /// Gets spawn rotation by player index.
    /// Called by CustomNetworkManager.
    /// </summary>
    public Quaternion GetSpawnRotationByIndex(int index) {
        if (spawnPoints != null && spawnPoints.Length > 0) {
            int spawnIndex = index % spawnPoints.Length;
            return spawnPoints[spawnIndex].rotation;
        }

        return Quaternion.identity;
    }
}
