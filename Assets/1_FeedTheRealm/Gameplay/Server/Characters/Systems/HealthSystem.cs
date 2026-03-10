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

        private float currentHealth;

        public float CurrentHealth => currentHealth;

        public event System.Action OnDeath;

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
            logger.Log("Character has died.", this);
            OnDeath?.Invoke();
        }
    }
}
