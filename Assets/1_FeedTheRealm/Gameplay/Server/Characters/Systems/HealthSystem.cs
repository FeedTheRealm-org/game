using System;
using FTR.Core.Common.Utils;
using FTR.Core.Server.EventChannels;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class HealthSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        public float MaxHealth = 100;

        [SerializeField]
        private Logging.Logger logger;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            if (resolver.TryResolve<PlayerHealEvent>(out var healEv) && healEv != null)
                playerHealEvent = healEv;
        }

        private PlayerHealEvent playerHealEvent;
        private bool subscribedToHealEvent = false;

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

            SubscribeToHealEvent();
        }

        private void OnDestroy()
        {
            UnsubscribeFromHealEvent();
        }

        private void SubscribeToHealEvent()
        {
            if (subscribedToHealEvent || playerHealEvent == null)
                return;
            playerHealEvent.OnRaised += OnHealEvent;
            subscribedToHealEvent = true;
        }

        private void UnsubscribeFromHealEvent()
        {
            if (!subscribedToHealEvent || playerHealEvent == null)
                return;
            playerHealEvent.OnRaised -= OnHealEvent;
            subscribedToHealEvent = false;
        }

        private void OnHealEvent((uint playerNetId, float amount) data)
        {
            if (data.playerNetId == netId)
            {
                Heal(data.amount);
            }
        }

        private void Awake()
        {
            currentHealth = MaxHealth;
        }

        public void GameTick(float dt) { }

        public void Heal(float amount)
        {
            if (currentHealth <= 0 || amount <= 0)
                return;

            currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
            stateStorage.SetHealth(currentHealth);
            logger?.Log(
                $"Character netId={netId} healed by {amount}. Health: {currentHealth}",
                this
            );
        }

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
