using System.Collections;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Entities;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    /// <summary>
    /// Handles death and respawn for a single server-side character.
    /// Subscribes to HealthSystem.OnDeath, unregisters the entity from the
    /// command loop while dead, then resets state and re-registers after the
    /// configured delay.
    /// Must be added to the ServerCharacterComponents prefab.
    /// </summary>
    public class RespawnSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private float respawnDelay = 3f;

        [SerializeField]
        private Logging.Logger logger;

        [Inject]
        private WorldMonitor world;

        // Captured in Initialize() and reused in the coroutine.
        private uint netId;
        private NetworkAdapter networkAdapter;
        private ServerCommandHandler commandHandler;
        private CharacterStateStorage stateStorage;
        private Rigidbody rb;
        private HealthSystem healthSystem;

        public void Initialize(
            uint netId,
            NetworkAdapter networkAdapter,
            ServerCommandHandler commandHandler,
            CharacterStateStorage stateStorage,
            Rigidbody rb,
            HealthSystem healthSystem
        )
        {
            this.netId = netId;
            this.networkAdapter = networkAdapter;
            this.commandHandler = commandHandler;
            this.stateStorage = stateStorage;
            this.rb = rb;
            this.healthSystem = healthSystem;

            healthSystem.OnDeath += OnDeath;
        }

        private void OnDestroy()
        {
            if (healthSystem != null)
                healthSystem.OnDeath -= OnDeath;
        }

        public void GameTick(float dt) { }

        // ── Death / Respawn ──────────────────────────────────────────────────

        private void OnDeath(uint _)
        {
            // Sync the 0 health to all clients immediately so UIs update.
            stateStorage.SetHealth(0);

            logger.Log(
                $"[RespawnSystem] Player {netId} died. Respawning in {respawnDelay}s.",
                this
            );

            StartCoroutine(RespawnCoroutine());
        }

        private IEnumerator RespawnCoroutine()
        {
            // Wait one frame so the current tick's FlushEventsToClients() can dispatch
            // the HitEvent(health=0) collected by UseSystem before we remove the entity.
            yield return null;
            world.Entities.Unregister(netId);

            yield return new WaitForSeconds(respawnDelay);

            healthSystem.ResetHealth();
            stateStorage.SetHealth(healthSystem.MaxHealth);

            // TODO: Replace with a spawn-point lookup (spawner provider could help, but need
            // spawn loading system to be implemented first).
            rb.position = Vector3.zero;
            rb.linearVelocity = Vector3.zero;

            world.Entities.Register(netId, new ServerEntity(netId, networkAdapter, commandHandler));

            world.Events.Enqueue(
                new HitEvent(netId, healthSystem.MaxHealth, healthSystem.MaxHealth)
            );

            logger.Log($"[RespawnSystem] Player {netId} respawned.", this);
        }
    }
}
