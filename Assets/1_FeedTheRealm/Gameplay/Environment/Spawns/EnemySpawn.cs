using UnityEngine;
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
        logger.Log($"{other.name} entered the area!", this);
        playersInside++;
        if (!spawnerActive && playersInside > 0) {
            spawnerActive = true;
            spawnRoutine = StartCoroutine(spawnEnemies());
        }
    }

    private void OnTriggerExit(Collider other) {
        logger.Log($"{other.name} exited the area!", this);
        playersInside--;
        if (playersInside == 0) {
            spawnerActive = false;
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
        while (spawnerActive) {
            if (currentEnemies < maxEnemies) {
                spawnEnemy();
            }
            yield return new WaitForSeconds(spawnRate);
        }
    }

    /// <summary>
    /// resets the spawner after a delay when the kill threshold is reached.
    /// </summary>
    private IEnumerator spawnReset() {
        logger.Log("Spawner resetting...", this);
        yield return new WaitForSeconds(resetDelay);
        spawnerResetting = false;
        totalKills = 0;
        logger.Log("Spawner reset complete.", this);
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

        Transform point = spawnPoints[spawnIndex];
        spawnIndex = (spawnIndex + 1) % spawnPoints.Length;

        GameObject enemy = Instantiate(enemyPrefab, point.position, point.rotation);
        enemy.GetComponent<HealthComponent>().OnDeath += onEnemyDeath;
        currentEnemies++;

        logger.Log($"Spawned enemy #{currentEnemies} at {point.name}", this);
    }

    /// <summary>
    /// Callback for enemy death event to decrement current enemy count.
    /// </summary>
    private void onEnemyDeath() {
        currentEnemies = Mathf.Max(0, currentEnemies - 1);
        totalKills++;
        if (!spawnerResetting && totalKills >= resetAfterKills) {
            spawnerResetting = true;
            StartCoroutine(spawnReset());
        }
        logger.Log($"An enemy has died. Current enemies: {currentEnemies}, Total kills: {totalKills}", this);
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
