using Models;
using UnityEngine;

/// <summary>
/// Simple component to mark and configure player spawn points.
/// Instances are created dynamically from WorldData.playerSpawnAreas.
/// NetworkManager discovers these via FindObjectsByType<PlayerSpawnPoint> when spawning players.
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    [Header("Spawn Configuration")]
    [SerializeField]
    private float radius = 1f;

    [Header("Ground Reference")]
    [SerializeField]
    private bool adjustToGroundHeight = true;

    [SerializeField]
    private float heightOffset = 0f;

    [Header("Logging")]
    [SerializeField]
    private Logging.Logger logger;

    /// <summary>
    /// Configures this spawn point from PlayerSpawnerData.
    /// Called after instantiation by loaders.
    /// </summary>
    public void ConfigureFromSpawnData(
        PlayerSpawnerData spawnData,
        Transform groundReference = null
    )
    {
        if (spawnData == null)
        {
            logger?.Log(
                "[PlayerSpawnPoint] ConfigureFromSpawnData called with null data!",
                this,
                Logging.LogType.Error
            );
            return;
        }

        radius = spawnData.Radius;

        Vector3 position = spawnData.Position;

        if (adjustToGroundHeight && groundReference != null)
        {
            position.y = groundReference.position.y + heightOffset;
        }

        transform.position = position;

        logger?.Log(
            $"[PlayerSpawnPoint] Configured spawn point at {position} with radius {radius}",
            this
        );
    }

    /// <summary>
    /// Gets the spawn position for this point.
    /// </summary>
    public Vector3 GetSpawnPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// Gets the spawn rotation for this point.
    /// </summary>
    public Quaternion GetSpawnRotation()
    {
        return transform.rotation;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);

        // Draw arrow to show forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
#endif
}
