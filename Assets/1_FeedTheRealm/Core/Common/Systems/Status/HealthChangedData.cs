namespace FTR.Core.Common.Systems.Status
{
    /// <summary>
    /// Payload for HealthChangedEvent.
    /// Carries the network identity of the affected entity plus its new health values.
    /// </summary>
    public struct HealthChangedData
    {
        /// <summary>Mirror netId of the entity whose health changed.</summary>
        public uint NetId;

        /// <summary>Health value after the change (can be 0 if dead).</summary>
        public float CurrentHealth;

        /// <summary>Maximum health of the entity.</summary>
        public float MaxHealth;

        public HealthChangedData(uint netId, float currentHealth, float maxHealth)
        {
            NetId = netId;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
        }
    }
}
