using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Manages spawn points for players in multiplayer.
/// Works with CustomNetworkManager to spawn players at correct positions.
/// </summary>
public class PlayerSpawnManager : NetworkBehaviour
{
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;

    [Header("Auto-detect Spawn Points")]
    [SerializeField]
    private bool autoDetectSpawnPoints = true;

    [SerializeField]
    private string spawnPointTag = "SpawnPoint";

    [Header("Ground Reference")]
    [SerializeField]
    private Transform groundReference;

    [SerializeField]
    private bool adjustToGroundHeight = true;

    [SerializeField]
    private float heightOffset = 0f;

    [SerializeField]
    private Logging.Logger logger;

    private void Awake()
    {
        if (autoDetectSpawnPoints)
        {
            DetectSpawnPoints();
        }
    }

    private void DetectSpawnPoints()
    {
        List<Transform> detectedSpawnPoints = new List<Transform>();

        foreach (Transform child in transform)
        {
            detectedSpawnPoints.Add(child);
        }

        GameObject[] taggedSpawnPoints = GameObject.FindGameObjectsWithTag(spawnPointTag);
        foreach (GameObject spawnObj in taggedSpawnPoints)
        {
            if (!detectedSpawnPoints.Contains(spawnObj.transform))
            {
                detectedSpawnPoints.Add(spawnObj.transform);
            }
        }

        if (detectedSpawnPoints.Count > 0)
        {
            spawnPoints = detectedSpawnPoints.ToArray();
            logger.Log(
                $"[PlayerSpawnManager] Detected {spawnPoints.Length} spawn points automatically",
                this
            );
        }
        else
        {
            logger.Log(
                "[PlayerSpawnManager] No spawn points detected. Using default position.",
                this,
                Logging.LogType.Warning
            );
        }
    }

    public override void OnStartServer()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            logger.Log(
                "[PlayerSpawnManager] No spawn points configured!",
                this,
                Logging.LogType.Error
            );
        }
        else
        {
            logger.Log(
                $"[PlayerSpawnManager] Spawn Manager activated with {spawnPoints.Length} spawn points",
                this
            );

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                logger.Log(
                    $"[PlayerSpawnManager] Spawn Point {i}: Position = {spawnPoints[i].position}, Rotation = {spawnPoints[i].rotation.eulerAngles}",
                    this
                );
            }
        }
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
