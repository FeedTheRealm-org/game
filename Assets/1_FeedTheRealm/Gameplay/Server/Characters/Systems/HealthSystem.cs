using System;
using FTR.Core.Common.Utils;
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

        private bool isInitialized = false;

        public void Initialize(uint netId)
        {
            this.netId = netId;
            isInitialized = true;
        }

        private void Awake()
        {
            currentHealth = MaxHealth;
        }

        public void GameTick(float dt) { }

        public bool TakeDamage(float damage)
        {
            if (currentHealth <= 0)
                return true;

            currentHealth -= damage;
            logger.Log($"Took {damage} damage, current health: {currentHealth}", this);
            var isDead = currentHealth <= 0;
            if (isDead)
                Die();

            return isDead;
        }

        public void ResetHealth()
        {
            currentHealth = MaxHealth;
            logger.Log($"Health reset to {MaxHealth}", this);
        }

        private void Die()
        {
            if (!isInitialized)
                return;
            logger.Log("Character has died.", this);
            OnDeath?.Invoke(netId);
        }
    }
}
