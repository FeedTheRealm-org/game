using System.Collections;
using FTR.Core.Common.Scopes;
using FTRShared.Runtime.Models;
using Mirror;
using UnityEngine;
using VContainer.Unity;

public class EnemySpawn : MonoBehaviour
{
    [Header("Spawn settings")]
    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private int maxEnemies = 3;

    [SerializeField]
    private float spawnRate = 2f;

    [SerializeField]
    private int resetAfterKills = 6;

    [SerializeField]
    private float resetDelay = 10f;

    [SerializeField]
    private CapsuleCollider spawnArea;

    [SerializeField]
    private ObjectResolverContainer resolverContainer;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    private Coroutine spawnRoutine;
    private bool spawnerActive;
    private int currentEnemies;
    private int playersInside;
    private int totalKills;

    private bool isInitialized = false;

    public void Initialize(EnemySpawnerData data)
    {
        maxEnemies = data.MaxEnemies;
        spawnRate = data.SpawnRate;
        resetAfterKills = data.ResetAfterKills;
        resetDelay = data.ResetDelay;
        spawnArea.radius = data.Radius;

        isInitialized = true;
    }

    private void OnEnable()
    {
        if (enemyPrefab == null)
            throw new System.Exception("Enemy prefab not assigned on EnemySpawn!");
    }

    private void OnDisable()
    {
        spawnerActive = false;
        playersInside = 0;
        totalKills = 0;
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playersInside == 0)
        {
            spawnerActive = true;
            spawnRoutine = StartCoroutine(SpawnEnemies());
        }

        playersInside++;
        logger.Log($"[EnemySpawn] Player entered. Total unique players: {playersInside}", this);
    }

    private void OnTriggerExit(Collider other)
    {
        logger.Log($"[EnemySpawn] Player exited. Total unique players: {playersInside - 1}", this);
        playersInside = Mathf.Max(0, playersInside - 1);

        if (playersInside == 0)
        {
            spawnerActive = false;
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }
        }
    }

    /// <summary>
    /// Spawns enemies at defined intervals while the spawner is active.
    /// </summary>
    private IEnumerator SpawnEnemies()
    {
        while (spawnerActive)
        {
            // Resetting (kill threshold reached)
            if (totalKills >= resetAfterKills)
            {
                logger.Log($"[EnemySpawn] Spawner is resetting, pausing spawns...", this);
                totalKills = 0;
                yield return new WaitForSeconds(resetDelay);
            }

            if (currentEnemies < maxEnemies)
                SpawnEnemy();
            currentEnemies++;

            yield return new WaitForSeconds(spawnRate);
        }
        logger.Log("[EnemySpawn] Spawn routine stopped.", this);
    }

    /// <summary>
    /// Handles enemy instantiation and listens on death event.
    /// </summary>
    private void SpawnEnemy()
    {
        logger.Log($"[EnemySpawn] Spawning enemy. Current enemies: {currentEnemies + 1}", this);
        Vector3 point = GetRandomPointInRadius();
        GameObject enemy = resolverContainer.Resolver?.Instantiate(
            enemyPrefab,
            point,
            Quaternion.identity
        );
        enemy.name = $"Enemy_{currentEnemies}";
        // TODO: Initialize enemy with reference to spawner for death callback
        NetworkServer.Spawn(enemy);
    }

    /// <summary>
    /// Callback for enemy death event to decrement current enemy count.
    /// </summary>
    private void OnEnemyDeath()
    {
        currentEnemies = Mathf.Max(0, currentEnemies - 1);
        totalKills++;
        logger.Log(
            $"[EnemySpawn] Enemy died. Enemies: {currentEnemies}, Kills: {totalKills}",
            this
        );
    }

    private Vector3 GetRandomPointInRadius()
    {
        Vector2 randomCircle = Random.insideUnitCircle * spawnArea.radius;
        return transform.position + new Vector3(randomCircle.x, spawnArea.center.y, randomCircle.y);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = spawnerActive ? Color.green : Color.red;
        Gizmos.matrix = transform.localToWorldMatrix; // Needed to translate to scene pos
        Gizmos.DrawWireSphere(spawnArea.center, spawnArea.radius);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
