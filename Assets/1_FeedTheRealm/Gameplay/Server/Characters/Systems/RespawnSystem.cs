using System.Collections;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Commands;
using FTR.Core.Server.Entities;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    /// <summary>
    /// Handles death and respawn for a single server-side character.
    /// Subscribes to HealthSystem.OnDeath, unregisters the entity from the
    /// command loop while dead, then resets state and re-registers after the
    /// configured delay.
    /// </summary>
    public class RespawnSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private float respawnDelay = 3f;

        [SerializeField]
        private Logging.Logger logger;

        [Inject]
        private WorldMonitor world;

        private uint netId;
        private NetworkAdapter networkAdapter;
        private ICommandable commandHandler;
        private Rigidbody rb;
        private HealthSystem healthSystem;

        public void Initialize(
            uint netId,
            NetworkAdapter networkAdapter,
            ICommandable commandHandler,
            Rigidbody rb,
            HealthSystem healthSystem
        )
        {
            this.netId = netId;
            this.networkAdapter = networkAdapter;
            this.commandHandler = commandHandler;
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

        private void OnDeath(uint _)
        {
            logger.Log(
                $"[RespawnSystem] Player {netId} died. Respawning in {respawnDelay}s.",
                this
            );
            StartCoroutine(RespawnCoroutine());
        }

        private IEnumerator RespawnCoroutine()
        {
            yield return null;
            world.Entities.Unregister(netId);

            yield return new WaitForSeconds(respawnDelay);

            healthSystem.ResetHealth();

            // TODO: Replace with a spawn-point lookup (spawner provider could help, but need
            // spawn loading system to be implemented first).
            rb.position = Vector3.zero;
            rb.linearVelocity = Vector3.zero;

            world.Entities.Register(netId, new ServerEntity(netId, networkAdapter, commandHandler));

            logger.Log($"[RespawnSystem] Player {netId} respawned.", this);
        }
    }
}
