using System;
using System.Collections;
using System.Collections.Generic;
using FTR.Core.Server.Config;
using FTR.Gameplay.Server.Reaper;
using Mirror;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Reaper
{
    /// <summary>
    /// Periodically checks registered entities and destroys those that are ready to be reaped.
    /// </summary>
    public class EntityReaper : MonoBehaviour
    {
        private readonly List<GameObject> reapableEntities = new();

        [Inject]
        private readonly ServerConfig config;

        [Inject]
        private readonly Logging.Logger logger;

        public void Register(GameObject entity)
        {
            if (entity == null)
            {
                logger.Log(
                    "[EntityReaper] Attempted to register a null entity.",
                    Logging.LogType.Error
                );
                return;
            }
            if (!entity.TryGetComponent(out IReapable reapable))
            {
                logger.Log(
                    $"[EntityReaper] {entity.name} does not implement IReapable, skipping registration",
                    Logging.LogType.Error
                );
                return;
            }
            reapableEntities.Add(entity);
            reapable.SpawnTime = DateTime.UtcNow;
            logger.Log(
                $"[EntityReaper] Entity {entity.name} registered. Total reapable entities: {reapableEntities.Count}."
            );
        }

        private void Start()
        {
            StartCoroutine(ReapRoutine());
        }

        private IEnumerator ReapRoutine()
        {
            var interval = new WaitForSeconds(config.ReapIntervalSeconds);

            while (true)
            {
                yield return interval;
                ReapEntities();
            }
        }

        private void ReapEntities()
        {
            logger.Log($"[EntityReaper] Checking {reapableEntities.Count} entities for reaping.");
            for (int i = reapableEntities.Count - 1; i >= 0; i--)
            {
                GameObject entity = reapableEntities[i];

                if (entity == null)
                {
                    reapableEntities.RemoveAt(i);
                    continue;
                }

                entity.TryGetComponent(out IReapable reapable);

                if (!IsSpawnTimeValid(reapable) || !reapable.CanReap())
                    continue;

                GameObject toDestroy =
                    entity.transform.parent != null ? entity.transform.parent.gameObject : entity;
                reapableEntities.RemoveAt(i);
                NetworkServer.Destroy(toDestroy);
            }
            logger.Log(
                $"[EntityReaper] Reaping complete. Remaining entities: {reapableEntities.Count}."
            );
        }

        private bool IsSpawnTimeValid(IReapable reapable)
        {
            if (reapable.SpawnTime == default || reapable.SpawnTime > DateTime.UtcNow)
                return false;

            return (DateTime.UtcNow - reapable.SpawnTime).TotalSeconds
                >= reapable.MinimumLifetimeSeconds;
        }
    }
}
