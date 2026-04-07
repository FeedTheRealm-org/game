using System.Collections;
using System.Collections.Generic;
using FTR.Core.Common.Scopes;
using FTR.Core.Server;
using FTR.Core.Server.Config;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.NetworkEntities.LootItem;
using FTR.Gameplay.Server.Characters.Systems;
using FTR.Gameplay.Server.Registry;
using FTRShared.Runtime.Models;
using Mirror;
using UnityEngine;
using UnityEngine.AI;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Environment.Spawns
{
    public class EnemySpawn : MonoBehaviour
    {
        [Header("Spawn settings")]
        [SerializeField]
        private GameObject enemyPrefab;

        [SerializeField]
        private int maxEnemies = 3;

        [SerializeField]
        private float enemyDestroyDelay = 3f;

        [SerializeField]
        private float spawnRate = 2f;

        [SerializeField]
        private int resetAfterKills = 6;

        [SerializeField]
        private float resetDelay = 10f;

        [SerializeField]
        private CapsuleCollider spawnArea;

        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private ObjectResolverContainer resolverContainer;

        private ServerPrefabProvider prefabProvider;
        private ServerConfig config;

        private Coroutine spawnRoutine;
        private bool spawnerActive;
        private int currentEnemies;
        private int playersInside;
        private int totalKills;
        private Dictionary<uint, GameObject> spawnedEnemies = new Dictionary<uint, GameObject>();

        private bool isInitialized = false;
        private bool navMeshReady = false;
        private string enemyId;
        private EnemyData enemyData;

        public void Initialize(EnemySpawnerData data, EnemyData enemyData)
        {
            if (data == null)
                throw new System.ArgumentNullException(
                    nameof(data),
                    "EnemySpawnerData cannot be null when initializing EnemySpawn."
                );

            config = resolverContainer.Resolver.Resolve<ServerConfig>();

            if (config == null)
                throw new System.ArgumentNullException(
                    nameof(config),
                    "ServerConfig cannot be null when initializing Spawn."
                );

            maxEnemies = data.MaxEnemies;
            spawnRate = data.SpawnRate;
            resetAfterKills = data.ResetAfterKills;
            resetDelay = data.ResetDelay;
            spawnArea.radius = data.Radius;
            this.enemyData = enemyData;
            enemyId = !string.IsNullOrEmpty(enemyData?.id) ? enemyData.id : data.EnemyId;

            if (string.IsNullOrEmpty(enemyId))
                Debug.LogWarning(
                    "[EnemySpawn] Enemy spawner initialized without a valid EnemyId.",
                    this
                );

            prefabProvider = resolverContainer.Resolver.Resolve<ServerPrefabProvider>();

            if (!isInitialized)
            {
                BuildNavMesh(data.Radius + 5f);
                isInitialized = true;
            }
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

            foreach (var kvp in spawnedEnemies)
            {
                if (kvp.Value == null)
                    continue;
                var healthSystem = kvp.Value.GetComponent<HealthSystem>();
                if (healthSystem != null)
                    healthSystem.OnDeath -= OnEnemyDeath;
            }
            spawnedEnemies.Clear();
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
            logger.Log(
                $"[EnemySpawn] Player exited. Total unique players: {playersInside - 1}",
                this
            );
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
                {
                    SpawnEnemy();
                    currentEnemies++;
                }

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
            GameObject enemy = resolverContainer.Resolver.Instantiate(
                enemyPrefab,
                point,
                Quaternion.identity
            );

            var characterId = !string.IsNullOrEmpty(enemyData?.id) ? enemyData.id : enemyId;
            var stateStorage = enemy.GetComponent<CharacterStateStorage>();
            if (stateStorage != null && !string.IsNullOrEmpty(characterId))
            {
                stateStorage.SetCharacterId(characterId);
            }
            else if (stateStorage == null)
            {
                Debug.LogWarning(
                    $"[EnemySpawn] CharacterStateStorage component not found on prefab for enemy '{enemyId}'."
                );
            }
            else
            {
                Debug.LogWarning(
                    "[EnemySpawn] CharacterId is empty, enemy sprite sync may fail.",
                    this
                );
            }

            NetworkServer.Spawn(enemy);

            enemy.name = $"Enemy_{currentEnemies}";
            var netId = enemy.GetComponent<NetworkIdentity>().netId;
            spawnedEnemies[netId] = enemy;
            var healthSystem = enemy.GetComponentInChildren<HealthSystem>();
            healthSystem.OnDeath += OnEnemyDeath;
        }

        /// <summary>
        /// Callback for enemy death event to decrement current enemy count.
        /// </summary>
        private void OnEnemyDeath(uint netId)
        {
            var enemy = spawnedEnemies[netId];
            spawnedEnemies.Remove(netId);
            StartCoroutine(DestroyEnemyAfterDelay(enemy, enemyDestroyDelay));

            currentEnemies = Mathf.Max(0, currentEnemies - 1);
            totalKills++;
            logger.Log(
                $"[EnemySpawn] Enemy died. Enemies: {currentEnemies}, Kills: {totalKills}",
                this
            );

            if (!string.IsNullOrEmpty(enemyId))
            {
                var currentEnemyData = enemyData ?? ServerItemsRegistry.GetEnemyById(enemyId);
                if (currentEnemyData != null && !string.IsNullOrEmpty(currentEnemyData.lootTableId))
                {
                    var lootTable = ServerItemsRegistry.GetLootTableById(
                        currentEnemyData.lootTableId
                    );
                    if (lootTable != null && lootTable.lootItems != null)
                    {
                        foreach (var lootEntry in lootTable.lootItems)
                        {
                            if (Random.Range(0, 100) < lootEntry.dropProbability)
                            {
                                SpawnLootItem(enemy.transform.position, lootEntry.id);
                            }
                        }
                    }
                }
            }
        }

        private void SpawnLootItem(Vector3 position, string itemId)
        {
            var lootItemPrefab = prefabProvider?.LootItemPrefab;
            if (lootItemPrefab != null)
            {
                GameObject lootInstance = resolverContainer.Resolver.Instantiate(
                    lootItemPrefab,
                    position,
                    Quaternion.identity
                );

                if (lootInstance != null)
                {
                    var stateStorage = lootInstance.GetComponent<LootItemStateStorage>();
                    if (stateStorage != null)
                    {
                        stateStorage.SetItemId(itemId);
                    }
                    NetworkServer.Spawn(lootInstance);
                }
            }
            else
            {
                logger.Log("[EnemySpawn] LootItem prefab not assigned on EnemySpawn!", this);
            }
        }

        private IEnumerator DestroyEnemyAfterDelay(GameObject enemy, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (enemy != null)
                Destroy(enemy);
        }

        private Vector3 GetRandomPointInRadius()
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnArea.radius;
            return transform.position
                + new Vector3(randomCircle.x, spawnArea.center.y, randomCircle.y);
        }

        /// <summary>
        /// Builds a NavMesh around the spawn area to ensure enemies can navigate properly.
        /// </summary>
        private void BuildNavMesh(float navMeshRadius)
        {
            Bounds bounds = new Bounds(transform.position, Vector3.one * navMeshRadius * 2);

            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(
                bounds,
                config.GroundLayer,
                NavMeshCollectGeometry.PhysicsColliders,
                0,
                new List<NavMeshBuildMarkup>(),
                sources
            );

            var obstacleSources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(
                bounds,
                config.ObstacleLayer,
                NavMeshCollectGeometry.PhysicsColliders,
                1, // Not Walkable
                new List<NavMeshBuildMarkup>(),
                obstacleSources
            );

            sources.AddRange(obstacleSources);

            var navMeshData = new NavMeshData();

            var buildOp = NavMeshBuilder.UpdateNavMeshDataAsync(
                navMeshData,
                UnityEngine.AI.NavMesh.GetSettingsByID(0),
                sources,
                bounds
            );

            UnityEngine.AI.NavMesh.AddNavMeshData(navMeshData);

            StartCoroutine(WaitForNavMesh(buildOp));
        }

        /// <summary>
        /// Waits for the NavMesh to be built before allowing enemy spawns.
        /// </summary>
        IEnumerator WaitForNavMesh(AsyncOperation buildOp)
        {
            while (!buildOp.isDone)
                yield return null;

            navMeshReady = true;
        }

#if DEBUG
        private IEnumerator Start()
        {
            yield return new WaitUntil(() => NetworkServer.active);

            var enemySpawnerData = new EnemySpawnerData(
                transform.position,
                spawnArea.radius,
                enemyId
            );
            var debugEnemyData = new EnemyData(
                enemyId,
                $"Enemy_{enemyId}",
                "A hostile enemy.",
                0,
                0,
                0,
                0,
                "",
                new Dictionary<string, string>()
            );
            Initialize(enemySpawnerData, debugEnemyData);
        }
#endif

        private void OnDrawGizmos()
        {
            Gizmos.color = spawnerActive ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, spawnArea.radius);

            if (!Application.isPlaying || !navMeshReady)
                return;

            Gizmos.color = Color.blue;

            var triangulation = NavMesh.CalculateTriangulation();

            for (int i = 0; i < triangulation.indices.Length; i += 3)
            {
                Vector3 v0 = triangulation.vertices[triangulation.indices[i]];
                Vector3 v1 = triangulation.vertices[triangulation.indices[i + 1]];
                Vector3 v2 = triangulation.vertices[triangulation.indices[i + 2]];

                Gizmos.DrawLine(v0, v1);
                Gizmos.DrawLine(v1, v2);
                Gizmos.DrawLine(v2, v0);
            }
        }
    }
}
