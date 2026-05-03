using System;
using System.Collections;
using FTR.Core.Common.Scopes;
using Mirror;
using UnityEngine;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Testing
{
    public class ThingSpawner : MonoBehaviour
    {
        [Header("Spawn settings")]
        [SerializeField]
        private GameObject thingPrefab;

        [SerializeField]
        private int maxAmount = 3;

        [SerializeField]
        private float spawnRate = 2f;

        [SerializeField]
        private ObjectResolverContainer resolverContainer;

        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;
        private Coroutine spawnRoutine;

        private string thingName = "Thing";

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => NetworkServer.active);
            if (thingPrefab == null)
                throw new Exception("Thing prefab not assigned on ThingSpawner!");
            logger.Log($"[{thingName}Spawner] Spawner Started.", this);
            thingName = thingPrefab.name;
            if (resolverContainer.Resolver == null)
            {
                resolverContainer.OnResolverSet += awaitResolverInitialization;
                yield break;
            }
            spawnRoutine = StartCoroutine(SpawnThings());
        }

        private void awaitResolverInitialization()
        {
            spawnRoutine = StartCoroutine(SpawnThings());
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
        private IEnumerator SpawnThings()
        {
            logger.Log($"[{thingName}Spawner] Spawn routine started.", this);
            int currentThings = 0;
            while (currentThings < maxAmount)
            {
                SpawnThing(currentThings);
                currentThings++;
                yield return new WaitForSeconds(spawnRate);
            }
            logger.Log($"[{thingName}Spawner] Spawn routine stopped.", this);
        }

        /// <summary>
        /// Handles thing instantiation and listens on death event.
        /// </summary>
        private void SpawnThing(int thingIndex = 0)
        {
            Vector3 point = transform.position + Vector3.up;
            GameObject Thing = resolverContainer.Resolver?.Instantiate(
                thingPrefab,
                point,
                Quaternion.identity
            );
            Thing.name = $"{thingName}-{thingIndex}";
            NetworkServer.Spawn(Thing);
        }
    }
}
