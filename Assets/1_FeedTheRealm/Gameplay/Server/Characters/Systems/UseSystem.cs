using System.Collections;
using FTR.Core.Common.Config;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class UseSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private float attackCooldown = 0.4f;

        [SerializeField]
        private int attackDamage = 40;

        [SerializeField]
        private float hitRadius;

        [SerializeField]
        private LayerMask targetLayer;

        [SerializeField]
        private Logging.Logger logger;

        private bool isAttacking = false;

        private Vector3 hitPoint = Vector3.zero;

        public void Initialize(Rigidbody rb)
        {
            this.hitPoint = rb.worldCenterOfMass;
        }

        public void GameTick(float dt) { }

        public void OnUse(IEventCollectable ec)
        {
            if (isAttacking)
                return;
            isAttacking = true;
            StartCoroutine(resetAttackCooldown());

            Collider[] hitTargets = Physics.OverlapSphere(hitPoint, hitRadius, targetLayer);
            foreach (Collider target in hitTargets)
            {
                logger.Log($"Hit target: {target.name}", this);
                var _ = target.GetComponent<HealthSystem>()?.TakeDamage(attackDamage);
                // if (isDead.HasValue && isDead.Value)
                //     enemySlayedEvent.Raise();
            }

            if (hitTargets.Length == 0)
            {
                logger.Log("No targets hit", this);
            }
        }

        /// <summary>
        /// Resets the attack cooldown after a delay.
        /// </summary>
        private IEnumerator resetAttackCooldown()
        {
            yield return new WaitForSeconds(attackCooldown);
            isAttacking = false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hitPoint, hitRadius);
        }
    }
}
