namespace FTR.Core.Common.Systems.Status
{
    /// <summary>
    /// Payload for HealthChangedEvent.
    /// Only raised for the local player's character; no NetId needed.
    /// </summary>
    public struct HealthChangedData
    {
        /// <summary>Health value after the change (can be 0 if dead).</summary>
        public float CurrentHealth;

        /// <summary>Maximum health of the entity.</summary>
        public float MaxHealth;

        public HealthChangedData(float currentHealth, float maxHealth)
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
        }
    }
}
