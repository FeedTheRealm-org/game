using System;
using Mirror;
using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    /*[SerializeField]
    private float maxHealth = 100f;

    [SerializeField]
    private Logging.Logger logger;

    private float currentHealth;
    private bool isInitialized = false;

    private Animator _animator;

    public event Action<float> OnHealthChanged;
    public event Action OnDeath;

    private void Awake()
    {
        // Initialize health early so NetworkHealthSynchronizer can access it
        currentHealth = maxHealth;
        isInitialized = true;
    }

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        if (isInitialized)
        {
            OnHealthChanged?.Invoke(currentHealth);
        }
    }

    /// <summary>
    /// Gets the current health value
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Sets the current health directly (used for network synchronization)
    /// Does NOT trigger animations or events
    /// </summary>
    public void SetCurrentHealth(float health)
    {
        currentHealth = health;
        OnHealthChanged?.Invoke(currentHealth);
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public bool TakeDamage(float damage)
    {
        currentHealth -= damage;
        var isDead = currentHealth <= 0;
        if (isDead)
            _animator.SetTrigger("4_Death");
        else
            _animator.SetTrigger("3_Damaged");
        OnHealthChanged?.Invoke(currentHealth);
        logger.Log($"Took {damage} damage, current health: {currentHealth}", this);
        return isDead;
    }

    public void Die()
    {
        logger.Log("Character has died.", this);
        OnDeath?.Invoke();

        // In multiplayer, use NetworkServer.Destroy instead of regular Destroy
        NetworkIdentity networkIdentity = GetComponent<NetworkIdentity>();
        if (networkIdentity != null && networkIdentity.netId != 0)
        {
            if (NetworkServer.active)
            {
                // Server: Destroy the NetworkIdentity (will replicate to clients)
                NetworkServer.Destroy(gameObject);
                logger.Log("NetworkIdentity destroyed by server.", this);
            }
            else
            {
                // Clients should never reach here in networked objects
                logger.Log(
                    "Client attempted to destroy NetworkIdentity - this should be handled by server!",
                    this,
                    Logging.LogType.Warning
                );
            }
        }
        else
        {
            // Singleplayer or non-networked object: Use normal Destroy
            Destroy(gameObject);
        }
    }*/
}
