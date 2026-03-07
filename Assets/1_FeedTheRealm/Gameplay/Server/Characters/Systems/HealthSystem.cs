using FTR.Core.Common.Config;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class HealthSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        public int MaxHealth = 100;

        [SerializeField]
        private Logging.Logger logger;

        private int currentHealth;

        private void Awake()
        {
            currentHealth = MaxHealth;
        }

        public void GameTick(float dt) { }

        public bool TakeDamage(int damage)
        {
            currentHealth -= damage;
            logger.Log($"Took {damage} damage, current health: {currentHealth}", this);
            var isDead = currentHealth <= 0;
            if (isDead)
                Die();

            return isDead;
        }

        public void Die()
        {
            logger.Log("Character has died.", this);
            // TODO: Replace with object pooling for better performance
            Destroy(gameObject); // TODO: Network destroy?
        }
    }
}
