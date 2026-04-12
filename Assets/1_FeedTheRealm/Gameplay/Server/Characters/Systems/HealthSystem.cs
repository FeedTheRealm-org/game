using System;
using FTR.Core.Common.Utils;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class HealthSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        public float MaxHealth = 100;

        [SerializeField]
        private Logging.Logger logger;

        public event Action<uint> OnDeath;

        public float CurrentHealth => currentHealth;

        private float currentHealth;
        private uint netId;
        private CharacterStateStorage stateStorage;

        private bool isInitialized = false;
        private bool isImmortal = false;

        public void Initialize(uint netId, CharacterStateStorage stateStorage, bool isImmortal)
        {
            this.netId = netId;
            this.stateStorage = stateStorage;
            this.isImmortal = isImmortal;
            isInitialized = true;
            stateStorage.SetHealth(currentHealth);
        }

        private void Awake()
        {
            currentHealth = MaxHealth;
        }

        public void GameTick(float dt) { }

        public (bool isDead, string characterId) TakeDamage(float damage, uint attackerNetId = 0)
        {
            if (isImmortal)
                return (false, null);

            if (currentHealth <= 0)
                return (false, null);

            currentHealth -= damage;
            stateStorage.SetHealth(Mathf.Max(0f, currentHealth));
            /*logger.Log(
                $"Took {damage} damage from netId={attackerNetId}, health: {currentHealth}",
                this
            );*/

            var isDead = currentHealth <= 0;
            if (isDead)
                Die(attackerNetId);

            return (isDead, stateStorage.CharacterId);
        }

        public void ResetHealth()
        {
            currentHealth = MaxHealth;
            stateStorage.SetHealth(MaxHealth);
            //logger.Log($"Health reset to {MaxHealth}", this);
        }

        private void Die(uint killerNetId)
        {
            if (!isInitialized)
                return;

            logger.Log($"Character has died. Killer netId={killerNetId}", this);
            OnDeath?.Invoke(netId);
        }
    }
}
