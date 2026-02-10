using Mirror;
using UnityEngine;

/// <summary>
/// Synchronizes health and death state for networked characters.
/// This component handles network synchronization while keeping HealthComponent as a pure MonoBehaviour.
/// Designed to work with enemies and any character that has a HealthComponent.
/// </summary>
public class NetworkHealthSynchronizer : NetworkBehaviour
{
    [SerializeField]
    private HealthComponent healthComponent;

    [SerializeField]
    private Logging.Logger logger;

    // SyncVars for network state
    [SyncVar(hook = nameof(OnHealthChanged))]
    private int networkHealth;

    [SyncVar(hook = nameof(OnIsDeadChanged))]
    private bool networkIsDead;

    private bool hasProcessedDeath = false;

    // Cache animator reference to avoid expensive GetComponentInChildren calls
    private Animator cachedAnimator;

    private void Awake()
    {
        if (healthComponent == null)
        {
            healthComponent = GetComponent<HealthComponent>();
        }

        // Cache animator reference
        cachedAnimator = GetComponentInChildren<Animator>();
    }

    public override void OnStartServer()
    {
        // Server: Initialize SyncVars with current health
        int currentHealth = healthComponent.GetCurrentHealth();
        networkHealth = currentHealth;
        networkIsDead = false;

        // Subscribe to HealthComponent events
        healthComponent.OnHealthChanged += OnHealthChangedServer;
        healthComponent.OnDeath += OnDeathServer;

        logger?.Log(
            $"[NetworkHealthSynchronizer] Server initialized for {gameObject.name} with health {currentHealth}/{healthComponent.MaxHealth}",
            this
        );
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            // Clients: Apply initial state
            ApplyHealthToComponent(networkHealth);

            if (networkIsDead && !hasProcessedDeath)
            {
                ProcessDeath();
            }

            logger?.Log(
                $"[NetworkHealthSynchronizer] Client initialized for {gameObject.name} with health {networkHealth}",
                this
            );
        }
    }

    public override void OnStopServer()
    {
        // Unsubscribe from HealthComponent events
        if (healthComponent != null)
        {
            healthComponent.OnHealthChanged -= OnHealthChangedServer;
            healthComponent.OnDeath -= OnDeathServer;
        }
    }

    #region Server Callbacks

    /// <summary>
    /// Called on server when HealthComponent health changes (e.g., takes damage)
    /// </summary>
    private void OnHealthChangedServer(float healthValue)
    {
        if (!isServer)
            return;

        int healthInt = Mathf.RoundToInt(healthValue);
        networkHealth = healthInt;

        logger?.Log(
            $"[NetworkHealthSynchronizer] Server: Health changed to {healthInt} for {gameObject.name}",
            this
        );
    }

    /// <summary>
    /// Called on server when HealthComponent triggers death
    /// </summary>
    private void OnDeathServer()
    {
        if (!isServer)
            return;

        networkIsDead = true;
        logger?.Log(
            $"[NetworkHealthSynchronizer] Server: Death triggered for {gameObject.name}",
            this
        );
    }

    #endregion

    #region SyncVar Hooks

    /// <summary>
    /// SyncVar hook called on clients when health changes
    /// </summary>
    private void OnHealthChanged(int oldValue, int newValue)
    {
        if (isServer)
            return; // Server already has the correct state

        // Apply the new health value
        ApplyHealthToComponent(newValue);

        // Trigger damage animation if health decreased
        if (newValue < oldValue && newValue > 0)
        {
            if (cachedAnimator != null)
            {
                cachedAnimator.SetTrigger("3_Damaged");
            }
        }
        // Trigger death animation if health reached 0
        else if (newValue <= 0)
        {
            if (cachedAnimator != null)
            {
                cachedAnimator.SetTrigger("4_Death");
            }
        }

        logger?.Log(
            $"[NetworkHealthSynchronizer] Client: Health changed from {oldValue} to {newValue} for {gameObject.name}",
            this
        );
    }

    /// <summary>
    /// SyncVar hook called on clients when death state changes
    /// </summary>
    private void OnIsDeadChanged(bool oldValue, bool newValue)
    {
        if (isServer)
            return; // Server already handled death

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
        if (healthComponent == null)
            return;

        // Set health directly without triggering animations
        healthComponent.SetCurrentHealth(healthValue);

        logger?.Log(
            $"[NetworkHealthSynchronizer] Applied health {healthValue} to {gameObject.name}",
            this
        );
    }

    /// <summary>
    /// Processes death on clients
    /// </summary>
    private void ProcessDeath()
    {
        if (hasProcessedDeath)
            return;
        hasProcessedDeath = true;

        if (healthComponent != null)
        {
            // On server: Let HealthComponent handle death normally (triggers animation + destruction)
            // On clients: Only trigger death animation, destruction will be handled by NetworkIdentity despawn
            if (isServer)
            {
                healthComponent.Die();
            }
            else
            {
                // Clients: Just play death animation without destroying
                // The NetworkIdentity despawn will handle cleanup automatically
                if (cachedAnimator != null)
                {
                    cachedAnimator.SetTrigger("4_Death");
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
        return networkHealth;
    }

    /// <summary>
    /// Gets the current synchronized death state
    /// </summary>
    public bool IsNetworkDead()
    {
        return networkIsDead;
    }

    #endregion
}
