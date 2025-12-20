using UnityEngine;
using Mirror;
using System.Collections;

public class EnemySpawn : MonoBehaviour {
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
    private Coroutine spawnRoutine;
    private bool spawnerActive;
    private bool spawnerResetting;

    private int currentEnemies;
    private int playersInside;
    private int totalKills;

    private void Start() {
        if (spawnPointContainer != null && (spawnPoints == null || spawnPoints.Length == 0)) {
            var children = spawnPointContainer.GetComponentsInChildren<Transform>();
            spawnPoints = System.Array.FindAll(children, t => t != spawnPointContainer);
        }

        if (spawnPoints == null || spawnPoints.Length == 0) {
            logger.Log("No spawn points found!", this, Logging.LogType.Warning);
        }
    }

    private void OnTriggerEnter(Collider other) {
        // In multiplayer, only server processes spawn triggers
        if (NetworkServer.active || NetworkClient.active) {
            if (!NetworkServer.active) {
                return; // Clients ignore spawn triggers
            }
        }

        logger.Log($"[EnemySpawn] {other.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)}) entered the area!", this);
        
        // Verify this is a player or relevant entity
        // You can add layer checks here if needed: if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
        
        playersInside++;
        if (!spawnerActive && playersInside > 0) {
            spawnerActive = true;
            spawnRoutine = StartCoroutine(spawnEnemies());
            logger.Log($"[EnemySpawn] Spawner activated! Players inside: {playersInside}", this);
        }
    }

    private void OnTriggerExit(Collider other) {
        // In multiplayer, only server processes spawn triggers
        if (NetworkServer.active || NetworkClient.active) {
            if (!NetworkServer.active) {
                return; // Clients ignore spawn triggers
            }
        }

        logger.Log($"[EnemySpawn] {other.name} exited the area!", this);
        playersInside--;
        if (playersInside == 0) {
            spawnerActive = false;
            logger.Log($"[EnemySpawn] Spawner deactivated! No players inside.", this);
            if (spawnRoutine != null) {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }
        }
    }

    /// <summary>
    /// spawns enemies at defined intervals while the spawner is active.
    /// </summary>
    private IEnumerator spawnEnemies() {
        logger.Log($"[EnemySpawn] Spawn routine started. Max enemies: {maxEnemies}, Spawn rate: {spawnRate}s, Reset after kills: {resetAfterKills}", this);
        while (spawnerActive) {
            // Check if spawner is resetting (kill threshold reached)
            if (spawnerResetting) {
                logger.Log($"[EnemySpawn] Spawner is resetting, pausing spawns...", this);
                yield return new WaitForSeconds(spawnRate);
                continue;
            }

            if (currentEnemies < maxEnemies) {
                spawnEnemy();
            } else {
                // Only log occasionally to avoid spam
                if (Time.frameCount % 100 == 0) {
                    logger.Log($"[EnemySpawn] Max enemies reached ({currentEnemies}/{maxEnemies}). Waiting...", this);
                }
            }
            yield return new WaitForSeconds(spawnRate);
        }
        logger.Log("[EnemySpawn] Spawn routine stopped.", this);
    }

    /// <summary>
    /// resets the spawner after a delay when the kill threshold is reached.
    /// </summary>
    private IEnumerator spawnReset() {
        logger.Log($"[EnemySpawn] Spawner resetting... (waiting {resetDelay}s)", this);
        yield return new WaitForSeconds(resetDelay);
        totalKills = 0;

        // After the cooldown ends, we only resume spawns if there are still
        // players inside the area (or if they entered again during the cooldown).
        if (playersInside <= 0) {
            // We ensure the spawner is completely turned off.
            spawnerActive = false;
            if (spawnRoutine != null) {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }

            spawnerResetting = false;
            logger.Log("[EnemySpawn] Spawner reset complete, no players inside. Staying idle until someone enters again.", this);
        } else {
            // There is at least one player inside; the spawner can remain active.
            spawnerResetting = false;

            // If for some reason the routine stopped, we resume it.
            if (!spawnerActive) {
                spawnerActive = true;
            }
            if (spawnRoutine == null) {
                spawnRoutine = StartCoroutine(spawnEnemies());
            }

            logger.Log("[EnemySpawn] Spawner reset complete. Players inside, resuming spawn.", this);
        }
    }

    /// <summary>
    /// Handles enemy instantiation and listens on death event.
    /// </summary>
    private void spawnEnemy() {
        if (enemyPrefab == null ||
            spawnPoints == null ||
            spawnPoints.Length == 0 ||
            spawnerResetting) {
            return;
        }

        // In multiplayer, only the server should spawn enemies
        if (NetworkServer.active || NetworkClient.active) {
            if (!NetworkServer.active) {
                logger.Log("Client attempted to spawn enemy - only server can spawn!", this, Logging.LogType.Warning);
                return;
            }
        }

        Transform point = spawnPoints[spawnIndex];
        spawnIndex = (spawnIndex + 1) % spawnPoints.Length;

        GameObject enemy = Instantiate(enemyPrefab, point.position, point.rotation);
        
        // Increment counter BEFORE spawning to get correct count in logs
        currentEnemies++;

        // Spawn as NetworkIdentity if in multiplayer
        if (NetworkServer.active || NetworkClient.active) {
            NetworkIdentity networkIdentity = enemy.GetComponent<NetworkIdentity>();
            if (networkIdentity != null) {
                NetworkServer.Spawn(enemy);
                logger.Log($"[EnemySpawn] Spawned networked enemy #{currentEnemies}/{maxEnemies} at {point.name}", this);
            } else {
                logger.Log("Enemy prefab missing NetworkIdentity component for multiplayer!", this, Logging.LogType.Error);
                Destroy(enemy);
                currentEnemies--; // Rollback counter
                return;
            }
        } else {
            logger.Log($"[EnemySpawn] Spawned local enemy #{currentEnemies}/{maxEnemies} at {point.name}", this);
        }

        // Subscribe to death event to track kills
        enemy.GetComponent<HealthComponent>().OnDeath += onEnemyDeath;
    }

    /// <summary>
    /// Callback for enemy death event to decrement current enemy count.
    /// </summary>
    private void onEnemyDeath() {
        // In multiplayer, only server should track enemy deaths for spawning logic
        if (NetworkServer.active || NetworkClient.active) {
            if (!NetworkServer.active) {
                return; // Clients don't manage spawn counts
            }
        }

        currentEnemies = Mathf.Max(0, currentEnemies - 1);
        totalKills++;
        logger.Log($"[EnemySpawn] Enemy died. Current enemies: {currentEnemies}, Total kills: {totalKills}", this);
        
        if (!spawnerResetting && totalKills >= resetAfterKills) {
            spawnerResetting = true;
            logger.Log($"[EnemySpawn] Kill threshold reached ({resetAfterKills}). Resetting spawner in {resetDelay}s...", this);
            StartCoroutine(spawnReset());
        }
    }

    private void OnDrawGizmos() {
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere == null) return;

        Gizmos.color = spawnerActive ? Color.green : Color.red;
        Gizmos.matrix = transform.localToWorldMatrix; // Needed to translate to scene pos
        Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
