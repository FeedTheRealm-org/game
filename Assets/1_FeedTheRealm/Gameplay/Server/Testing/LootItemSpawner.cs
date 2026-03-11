using System.Collections;
using FTR.Core.Common.Scopes;
using Mirror;
using UnityEngine;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Testing
{
    public class LootItemSpawner : MonoBehaviour
    {
        [Header("Spawn settings")]
        [SerializeField]
        private GameObject lootPrefab;

        [SerializeField]
        private int maxLoots = 3;

        [SerializeField]
        private float spawnRate = 2f;

        [SerializeField]
        private ObjectResolverContainer resolverContainer;

        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;
        private Coroutine spawnRoutine;

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => NetworkServer.active);
            logger.Log("[LootSpawn] Spawner Started.", this);
            if (lootPrefab == null)
                throw new System.Exception("Loot prefab not assigned on LootSpawner!");

            if (resolverContainer.Resolver == null)
            {
                resolverContainer.OnResolverSet += awaitResolverInitialization;
                yield break;
            }
            spawnRoutine = StartCoroutine(SpawnLoots());
        }

        private void awaitResolverInitialization()
        {
            spawnRoutine = StartCoroutine(SpawnLoots());
        }

        private void OnDisable()
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }
        }

        /// <summary>
        /// Spawns loot items at defined intervals while the spawner is active.
        /// </summary>
        private IEnumerator SpawnLoots()
        {
            logger.Log("[LootSpawn] Spawn routine started.", this);
            int currentLoots = 0;
            while (currentLoots < maxLoots)
            {
                SpawnLoot(currentLoots);
                currentLoots++;
                logger.Log($"[LootSpawn] Spawning loot. Current loot: {currentLoots}", this);
                yield return new WaitForSeconds(spawnRate);
            }
            logger.Log("[LootSpawn] Spawn routine stopped.", this);
        }

        /// <summary>
        /// Handles loot instantiation and listens on death event.
        /// </summary>
        private void SpawnLoot(int lootIndex = 0)
        {
            Vector3 point = transform.position;
            GameObject Loot = resolverContainer.Resolver?.Instantiate(
                lootPrefab,
                point,
                Quaternion.identity
            );
            Loot.name = $"LootItem-{lootIndex}";
            NetworkServer.Spawn(Loot);
        }
    }
}
