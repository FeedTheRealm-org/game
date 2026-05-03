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
    /// Subscribes to HealthSystem.OnDeath
    /// </summary>
    public class RespawnSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private float respawnDelay = 3f;

        [SerializeField]
        private Logging.Logger logger;

        [Inject]
        private WorldMonitor world;

        [Inject]
        PlayerSpawnpointManager playerSpawnpointManager;

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
            yield return new WaitForSeconds(respawnDelay);

            healthSystem.ResetHealth();

            rb.position = playerSpawnpointManager.GetRandomSpawnpoint();

            logger.Log($"[RespawnSystem] Player {netId} respawned.", this);
        }
    }
}
