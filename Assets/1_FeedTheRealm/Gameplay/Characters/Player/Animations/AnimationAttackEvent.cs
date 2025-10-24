using UnityEngine;

public class AnimationAttackEvent : MonoBehaviour {

    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private AttackComponent attackComponent;

    void Attack() {
        attackComponent.DetectAttackHit();
    }
}
