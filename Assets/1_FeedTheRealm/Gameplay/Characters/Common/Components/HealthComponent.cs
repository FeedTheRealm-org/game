using UnityEngine;

public class HealthComponent : MonoBehaviour {
    [SerializeField]
    private int maxHealth = 100;

    [SerializeField]
    private Logging.Logger logger;

    private int currentHealth;

    private Animator _animator;

    private void Start() {
        _animator = GetComponentInChildren<Animator>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage) {
        currentHealth -= damage;
        if (currentHealth <= 0) {
            _animator.SetTrigger("4_Death");
        } else {
            _animator.SetTrigger("3_Damaged");
        }
        logger.Log($"Took {damage} damage, current health: {currentHealth}", this);
    }

    public void Die() {
        logger.Log("Character has died.", this);
        Destroy(gameObject);
    }
}
