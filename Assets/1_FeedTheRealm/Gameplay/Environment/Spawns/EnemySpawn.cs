using System.Collections;
using Mirror;
using UnityEngine;

public class EnemySpawn : MonoBehaviour
{
    [Header("Spawn settings")]
    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private int maxEnemies = 3;

    [SerializeField]
    private float spawnRate = 2f;

    [Header("Spawn settings")]
    [SerializeField]
    private int resetAfterKills = 6;

    [SerializeField]
    private float resetDelay = 10f;

    [Header("Spawn points settings")]
    [SerializeField]
    private Transform spawnPointContainer;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    private Transform[] spawnPoints;
    private int spawnIndex = 0;
    private System.Threading.CancellationTokenSource spawnCts;
    private bool spawnerActive;
    private bool spawnerResetting;

    private int currentEnemies;
    private int playersInside;
    private System.Collections.Generic.HashSet<uint> playersInArea =
        new System.Collections.Generic.HashSet<uint>();
    private int totalKills;

    private void Start()
    {
        if (spawnPointContainer != null && (spawnPoints == null || spawnPoints.Length == 0))
        {
            var children = spawnPointContainer.GetComponentsInChildren<Transform>();
            spawnPoints = System.Array.FindAll(children, t => t != spawnPointContainer);
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            logger.Log("No spawn points found!", this, Logging.LogType.Warning);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // In multiplayer, only server processes spawn triggers
        if (NetworkServer.active || NetworkClient.active)
        {
            if (!NetworkServer.active)
            {
                return; // Clients ignore spawn triggers
            }
        }

        if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        // Get unique player identifier for multiplayer
        NetworkIdentity netId = other.GetComponent<NetworkIdentity>();
        if (netId == null)
        {
            logger.Log(
                $"[EnemySpawn] {other.name} has no NetworkIdentity, ignoring.",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        uint playerId = netId.netId;
        logger.Log($"[EnemySpawn] {other.name} (netId: {playerId}) entered the area!", this);

        if (playersInArea.Add(playerId))
        {
            playersInside = playersInArea.Count;
            logger.Log(
                $"[EnemySpawn] Player {playerId} added to area. Total unique players: {playersInside}",
                this
            );

            if (!spawnerActive && playersInside > 0)
            {
                spawnerActive = true;
                logger.Log(
                    $"[EnemySpawn] Spawner activated! Players inside: {playersInside}",
                    this
                );
            }

            // Only start spawn task if not already running
            if (spawnCts == null && spawnerActive)
            {
                spawnCts = new System.Threading.CancellationTokenSource();
                _ = SpawnEnemiesAsync(spawnCts.Token);
                logger.Log($"[EnemySpawn] Spawn task created.", this);
            }
        }
        else
        {
            logger.Log(
                $"[EnemySpawn] Player {playerId} already in area (duplicate trigger).",
                this
            );
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // In multiplayer, only server processes spawn triggers
        if (NetworkServer.active || NetworkClient.active)
        {
            if (!NetworkServer.active)
            {
                return; // Clients ignore spawn triggers
            }
        }

        if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        // Get unique player identifier for multiplayer
        NetworkIdentity netId = other.GetComponent<NetworkIdentity>();
        if (netId == null)
        {
            logger.Log(
                $"[EnemySpawn] {other.name} has no NetworkIdentity on exit, ignoring.",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        uint playerId = netId.netId;
        logger.Log($"[EnemySpawn] {other.name} (netId: {playerId}) exited the area!", this);

        if (playersInArea.Remove(playerId))
        {
            playersInside = playersInArea.Count;
            logger.Log(
                $"[EnemySpawn] Player {playerId} removed from area. Remaining players: {playersInside}",
                this
            );

            if (playersInside == 0)
            {
                spawnerActive = false;
                logger.Log($"[EnemySpawn] Spawner deactivated! No players inside.", this);
                if (spawnCts != null)
                {
                    spawnCts.Cancel();
                    spawnCts.Dispose();
                    spawnCts = null;
                }
                // Don't reset currentEnemies here - enemies still exist in world
                // They will decrement naturally as they die
            }
        }
        else
        {
            logger.Log(
                $"[EnemySpawn] Player {playerId} not found in area (duplicate exit trigger).",
                this
            );
        }
    }

    /// <summary>
    /// spawns enemies at defined intervals while the spawner is active.
    /// </summary>
    private async System.Threading.Tasks.Task SpawnEnemiesAsync(
        System.Threading.CancellationToken token
    )
    {
        logger.Log(
            $"[EnemySpawn] Spawn task started. Max enemies: {maxEnemies}, Spawn rate: {spawnRate}s, Reset after kills: {resetAfterKills}",
            this
        );

        // Sanity check: clamp currentEnemies to [0, maxEnemies] at start
        if (currentEnemies < 0 || currentEnemies > maxEnemies)
        {
            logger.Log(
                $"[EnemySpawn] Corrigiendo currentEnemies de {currentEnemies} a rango [0, {maxEnemies}] al iniciar rutina.",
                this,
                Logging.LogType.Warning
            );
            currentEnemies = Mathf.Clamp(currentEnemies, 0, maxEnemies);
        }
        try
        {
            while (spawnerActive && !token.IsCancellationRequested)
            {
                // Check if spawner is resetting (kill threshold reached)
                if (spawnerResetting)
                {
                    logger.Log($"[EnemySpawn] Spawner is resetting, pausing spawns...", this);
                    await System.Threading.Tasks.Task.Delay((int)(spawnRate * 1000), token);
                    continue;
                }

                // Clamp currentEnemies to [0, maxEnemies] before each spawn attempt
                if (currentEnemies < 0 || currentEnemies > maxEnemies)
                {
                    logger.Log(
                        $"[EnemySpawn] Corrigiendo currentEnemies de {currentEnemies} a rango [0, {maxEnemies}] en ciclo.",
                        this,
                        Logging.LogType.Warning
                    );
                    currentEnemies = Mathf.Clamp(currentEnemies, 0, maxEnemies);
                }

                if (currentEnemies < maxEnemies && !spawnerResetting)
                {
                    spawnEnemy();
                }
                else if (currentEnemies >= maxEnemies)
                {
                    // Only log occasionally to avoid spam
                    if (Time.frameCount % 100 == 0)
                    {
                        logger.Log(
                            $"[EnemySpawn] Max enemies reached ({currentEnemies}/{maxEnemies}). Waiting...",
                            this
                        );
                    }
                }
                await System.Threading.Tasks.Task.Delay((int)(spawnRate * 1000), token);
            }
        }
        catch (System.OperationCanceledException) { }
        logger.Log("[EnemySpawn] Spawn task stopped.", this);
    }

    /// <summary>
    /// resets the spawner after a delay when the kill threshold is reached.
    /// </summary>
    private async System.Threading.Tasks.Task SpawnResetAsync()
    {
        logger.Log($"[EnemySpawn] Spawner resetting... (waiting {resetDelay}s)", this);
        await System.Threading.Tasks.Task.Delay((int)(resetDelay * 1000));
        totalKills = 0;

        // Sanity check: clamp currentEnemies after reset
        if (currentEnemies < 0 || currentEnemies > maxEnemies)
        {
            logger.Log(
                $"[EnemySpawn] Corrigiendo currentEnemies de {currentEnemies} a rango [0, {maxEnemies}] tras reset.",
                this,
                Logging.LogType.Warning
            );
            currentEnemies = Mathf.Clamp(currentEnemies, 0, maxEnemies);
        }

        // After the cooldown ends, we only resume spawns if there are still
        // players inside the area (or if they entered again during the cooldown).
        if (playersInside <= 0)
        {
            // We ensure the spawner is completely turned off.
            spawnerActive = false;
            if (spawnCts != null)
            {
                spawnCts.Cancel();
                spawnCts.Dispose();
                spawnCts = null;
            }

            spawnerResetting = false;
            logger.Log(
                "[EnemySpawn] Spawner reset complete, no players inside. Staying idle until someone enters again.",
                this
            );
        }
        else
        {
            // There is at least one player inside; the spawner can remain active.
            spawnerResetting = false;

            if (!spawnerActive)
            {
                spawnerActive = true;
            }
            if (spawnCts == null)
            {
                spawnCts = new System.Threading.CancellationTokenSource();
                _ = SpawnEnemiesAsync(spawnCts.Token);
                logger.Log(
                    "[EnemySpawn] Spawner reset complete. Players inside, resuming spawn.",
                    this
                );
            }
        }
    }

    /// <summary>
    /// Handles enemy instantiation and listens on death event.
    /// </summary>
    private void spawnEnemy()
    {
        if (
            enemyPrefab == null
            || spawnPoints == null
            || spawnPoints.Length == 0
            || spawnerResetting
        )
        {
            return;
        }

        // In multiplayer, only the server should spawn enemies
        if (NetworkServer.active || NetworkClient.active)
        {
            if (!NetworkServer.active)
            {
                logger.Log(
                    "Client attempted to spawn enemy - only server can spawn!",
                    this,
                    Logging.LogType.Warning
                );
                return;
            }
        }

        Transform point = spawnPoints[spawnIndex];
        spawnIndex = (spawnIndex + 1) % spawnPoints.Length;

        GameObject enemy = Instantiate(enemyPrefab, point.position, point.rotation);

        // Increment counter BEFORE spawning to get correct count in logs
        currentEnemies++;

        // Spawn as NetworkIdentity if in multiplayer
        if (NetworkServer.active || NetworkClient.active)
        {
            NetworkIdentity networkIdentity = enemy.GetComponent<NetworkIdentity>();
            if (networkIdentity != null)
            {
                NetworkServer.Spawn(enemy);
                logger.Log(
                    $"[EnemySpawn] Spawned networked enemy #{currentEnemies}/{maxEnemies} at {point.name}",
                    this
                );
            }
            else
            {
                logger.Log(
                    "Enemy prefab missing NetworkIdentity component for multiplayer!",
                    this,
                    Logging.LogType.Error
                );
                Destroy(enemy);
                currentEnemies--; // Rollback counter
                return;
            }
        }
        else
        {
            logger.Log(
                $"[EnemySpawn] Spawned local enemy #{currentEnemies}/{maxEnemies} at {point.name}",
                this
            );
        }

        // Subscribe to death event to track kills
        enemy.GetComponent<HealthComponent>().OnDeath += onEnemyDeath;
    }

    /// <summary>
    /// Callback for enemy death event to decrement current enemy count.
    /// </summary>
    private void onEnemyDeath()
    {
        // In multiplayer, only server should track enemy deaths for spawning logic
        if (NetworkServer.active || NetworkClient.active)
        {
            if (!NetworkServer.active)
            {
                return; // Clients don't manage spawn counts
            }
        }

        currentEnemies = Mathf.Max(0, currentEnemies - 1);
        totalKills++;
        logger.Log(
            $"[EnemySpawn] Enemy died. Current enemies: {currentEnemies}, Total kills: {totalKills}",
            this
        );

        if (!spawnerResetting && totalKills >= resetAfterKills)
        {
            spawnerResetting = true;
            logger.Log(
                $"[EnemySpawn] Kill threshold reached ({resetAfterKills}). Resetting spawner in {resetDelay}s...",
                this
            );
            _ = SpawnResetAsync();
        }
    }

    /// <summary>
    /// Configures this spawn instance with data from EnemySpawnAreaData.
    /// Must be called after instantiation for dynamically placed spawns.
    /// </summary>
    public void ConfigureFromSpawnData(Models.EnemySpawnAreaData spawnData)
    {
        if (spawnData == null)
        {
            logger?.Log(
                "[EnemySpawn] ConfigureFromSpawnData called with null data!",
                this,
                Logging.LogType.Error
            );
            return;
        }

        // Configure spawn parameters
        maxEnemies = spawnData.MaxEnemies;
        spawnRate = spawnData.SpawnRate;
        resetAfterKills = spawnData.ResetAfterKills;
        resetDelay = spawnData.ResetDelay;

        // Configure collider radius
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere != null)
        {
            sphere.radius = spawnData.Radius;
            logger?.Log(
                $"[EnemySpawn] Configured spawn: radius={spawnData.Radius}, maxEnemies={maxEnemies}, spawnRate={spawnRate}s, resetAfterKills={resetAfterKills}",
                this
            );
        }
        else
        {
            logger?.Log(
                "[EnemySpawn] No SphereCollider found on spawn instance!",
                this,
                Logging.LogType.Warning
            );
        }
    }

    private void OnDrawGizmos()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere == null)
            return;

        Gizmos.color = spawnerActive ? Color.green : Color.red;
        Gizmos.matrix = transform.localToWorldMatrix; // Needed to translate to scene pos
        Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
