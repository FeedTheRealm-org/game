using UnityEngine;
using System;

public class HealthComponent : MonoBehaviour {
    [SerializeField]
    public int MaxHealth = 100;

    [SerializeField]
    private Logging.Logger logger;

    private int currentHealth;

    private Animator _animator;

    public event Action<float> OnHealthChanged;
    public event Action OnDeath;

    private void Start() {
        _animator = GetComponentInChildren<Animator>();
        currentHealth = MaxHealth;
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
        Destroy(gameObject);
    }
}
