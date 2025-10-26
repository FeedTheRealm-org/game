using UnityEngine;

public class AnimationEvents : MonoBehaviour {

    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private AttackComponent attackComponent;

    [SerializeField]
    private HealthComponent healthComponent;

    private void Attack() {
        if (attackComponent == null) {
            logger.Log("AttackComponent reference is missing!", this, Logging.LogType.Error);
            return;
        }
        attackComponent.DetectAttackHit();
    }

    private void Die() {
        if (healthComponent == null) {
            logger.Log("HealthComponent reference is missing!", this);
            return;
        }
        healthComponent.Die();
    }
}
