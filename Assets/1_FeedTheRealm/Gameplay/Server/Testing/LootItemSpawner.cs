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
        private GameObject thingPrefab;

        [SerializeField]
        private int maxThings = 3;

        [SerializeField]
        private float spawnRate = 2f;

        [SerializeField]
        private ObjectResolverContainer resolverContainer;

        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;
        private Coroutine spawnRoutine;

        private void Start()
        {
            logger.Log("[ThingSpawn] Spawner enabled.", this);
            if (thingPrefab == null)
                throw new System.Exception("Thing prefab not assigned on ThingSpawner!");

            if (resolverContainer.Resolver == null)
            {
                resolverContainer.OnResolverSet += awaitResolverInitialization;
                return;
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
            int currentThings = 0;
            while (currentThings < maxThings)
            {
                SpawnLoot();
                currentThings++;
                logger.Log($"[LootSpawn] Spawning loot. Current loot: {currentThings}", this);
                yield return new WaitForSeconds(spawnRate);
            }
            logger.Log("[LootSpawn] Spawn routine stopped.", this);
        }

        /// <summary>
        /// Handles loot instantiation and listens on death event.
        /// </summary>
        private void SpawnLoot()
        {
            Vector3 point = transform.position;
            GameObject thing = resolverContainer.Resolver?.Instantiate(
                thingPrefab,
                point,
                Quaternion.identity
            );
            NetworkServer.Spawn(thing);
        }
    }
}
