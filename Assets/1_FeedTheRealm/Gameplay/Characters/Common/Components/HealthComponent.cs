using UnityEngine;
using Unity.Netcode;
using System;

public class HealthComponent : MonoBehaviour {
    [SerializeField]
    public int MaxHealth = 100;

    [SerializeField]
    private Logging.Logger logger;

    private int currentHealth;
    private bool isInitialized = false;

    private Animator _animator;

    public event Action<float> OnHealthChanged;
    public event Action OnDeath;

    private void Awake() {
        // Initialize health early so NetworkHealthSynchronizer can access it
        currentHealth = MaxHealth;
        isInitialized = true;
    }

    private void Start() {
        _animator = GetComponentInChildren<Animator>();
        if (isInitialized) {
            OnHealthChanged?.Invoke(currentHealth);
        }
    }

    /// <summary>
    /// Gets the current health value
    /// </summary>
    public int GetCurrentHealth() {
        return currentHealth;
    }

    /// <summary>
    /// Sets the current health directly (used for network synchronization)
    /// Does NOT trigger animations or events
    /// </summary>
    public void SetCurrentHealth(int health) {
        currentHealth = health;
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void TakeDamage(int damage) {
        currentHealth -= damage;
        if (currentHealth <= 0) {
            _animator.SetTrigger("4_Death");
        } else {
            _animator.SetTrigger("3_Damaged");
        }
        OnHealthChanged?.Invoke(currentHealth);
        logger.Log($"Took {damage} damage, current health: {currentHealth}", this);
    }

    public void Die() {
        logger.Log("Character has died.", this);
        OnDeath?.Invoke();
        
        // In multiplayer, use NetworkObject.Despawn instead of Destroy
        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                // Server: Despawn the NetworkObject (will replicate to clients)
                networkObject.Despawn(true); // true = destroy after despawn
                logger.Log("NetworkObject despawned by server.", this);
            }
            else
            {
                // Clients should never reach here in networked objects
                logger.Log("Client attempted to despawn NetworkObject - this should be handled by server!", this, Logging.LogType.Warning);
            }
        }
        else
        {
            // Singleplayer or non-networked object: Use normal Destroy
            Destroy(gameObject);
        }
    }
}
