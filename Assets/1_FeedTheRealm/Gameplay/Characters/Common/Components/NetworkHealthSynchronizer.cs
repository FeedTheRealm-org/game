using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Synchronizes health and death state for networked characters.
/// This component handles network synchronization while keeping HealthComponent as a pure MonoBehaviour.
/// Designed to work with enemies and any character that has a HealthComponent.
/// </summary>
public class NetworkHealthSynchronizer : NetworkBehaviour
{
    [SerializeField] private HealthComponent healthComponent;
    [SerializeField] private Logging.Logger logger;

    // Network state
    private NetworkVariable<int> networkHealth = new NetworkVariable<int>(
        writePerm: NetworkVariableWritePermission.Server
    );
    
    private NetworkVariable<bool> networkIsDead = new NetworkVariable<bool>(
        writePerm: NetworkVariableWritePermission.Server
    );

    private bool hasProcessedDeath = false;

    private void Awake()
    {
        if (healthComponent == null)
        {
            healthComponent = GetComponent<HealthComponent>();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Server: Initialize network variables with current health
            int currentHealth = healthComponent.GetCurrentHealth();
            networkHealth.Value = currentHealth;
            networkIsDead.Value = false;

            healthComponent.OnHealthChanged += OnHealthChangedServer;
            healthComponent.OnDeath += OnDeathServer;

            logger?.Log($"[NetworkHealthSynchronizer] Server initialized for {gameObject.name} with health {currentHealth}/{healthComponent.MaxHealth}", this);
        }
        else
        {
            // Clients: Subscribe to network variable changes
            networkHealth.OnValueChanged += OnHealthChangedClient;
            networkIsDead.OnValueChanged += OnIsDeadChangedClient;

            // Apply initial state
            ApplyHealthToComponent(networkHealth.Value);
            if (networkIsDead.Value && !hasProcessedDeath)
            {
                ProcessDeath();
            }

            logger?.Log($"[NetworkHealthSynchronizer] Client initialized for {gameObject.name} with health {networkHealth.Value}", this);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && healthComponent != null)
        {
            healthComponent.OnHealthChanged -= OnHealthChangedServer;
            healthComponent.OnDeath -= OnDeathServer;
        }
        else if (!IsServer)
        {
            networkHealth.OnValueChanged -= OnHealthChangedClient;
            networkIsDead.OnValueChanged -= OnIsDeadChangedClient;
        }
    }

    #region Server Callbacks

    /// <summary>
    /// Called on server when HealthComponent health changes (e.g., takes damage)
    /// </summary>
    private void OnHealthChangedServer(float healthValue)
    {
        if (!IsServer) return;

        int healthInt = Mathf.RoundToInt(healthValue);
        networkHealth.Value = healthInt;

        logger?.Log($"[NetworkHealthSynchronizer] Server: Health changed to {healthInt} for {gameObject.name}", this);
    }

    /// <summary>
    /// Called on server when HealthComponent triggers death
    /// </summary>
    private void OnDeathServer()
    {
        if (!IsServer) return;

        networkIsDead.Value = true;
        logger?.Log($"[NetworkHealthSynchronizer] Server: Death triggered for {gameObject.name}", this);
    }

    #endregion

    #region Client Callbacks

    /// <summary>
    /// Called on clients when network health changes
    /// </summary>
    private void OnHealthChangedClient(int oldValue, int newValue)
    {
        if (IsServer) return; // Server already has the correct state

        // Apply the new health value
        ApplyHealthToComponent(newValue);
        
        // Trigger damage animation if health decreased
        if (newValue < oldValue && newValue > 0)
        {
            var animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("3_Damaged");
            }
        }
        // Trigger death animation if health reached 0
        else if (newValue <= 0)
        {
            var animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("4_Death");
            }
        }
        
        logger?.Log($"[NetworkHealthSynchronizer] Client: Health changed from {oldValue} to {newValue} for {gameObject.name}", this);
    }

    /// <summary>
    /// Called on clients when network death state changes
    /// </summary>
    private void OnIsDeadChangedClient(bool oldValue, bool newValue)
    {
        if (IsServer) return; // Server already handled death

        if (newValue && !hasProcessedDeath)
        {
            ProcessDeath();
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Applies health value to the HealthComponent without triggering damage animation
    /// </summary>
    private void ApplyHealthToComponent(int healthValue)
    {
        if (healthComponent == null) return;

        // Set health directly without triggering animations
        healthComponent.SetCurrentHealth(healthValue);
        
        logger?.Log($"[NetworkHealthSynchronizer] Applied health {healthValue} to {gameObject.name}", this);
    }

    /// <summary>
    /// Processes death on clients
    /// </summary>
    private void ProcessDeath()
    {
        if (hasProcessedDeath) return;
        hasProcessedDeath = true;

        if (healthComponent != null)
        {
            // On server: Let HealthComponent handle death normally (triggers animation + destruction)
            // On clients: Only trigger death animation, destruction will be handled by NetworkObject despawn
            if (IsServer)
            {
                healthComponent.Die();
            }
            else
            {
                // Clients: Just play death animation without destroying
                // The NetworkObject despawn will handle cleanup automatically
                var animator = GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger("4_Death");
                }
            }
        }

        logger?.Log($"[NetworkHealthSynchronizer] Death processed for {gameObject.name}", this);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gets the current synchronized health value
    /// </summary>
    public int GetNetworkHealth()
    {
        return networkHealth.Value;
    }

    /// <summary>
    /// Gets the current synchronized death state
    /// </summary>
    public bool IsNetworkDead()
    {
        return networkIsDead.Value;
    }

    #endregion
}
