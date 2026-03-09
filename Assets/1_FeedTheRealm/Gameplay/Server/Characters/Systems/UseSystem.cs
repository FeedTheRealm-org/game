using System.Collections;
using FTR.Core.Common.Config;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Server.Characters.Systems;
using Mirror;
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

        private Rigidbody _rb;
        private uint netId;

        private Vector3 HitPoint => _rb != null ? _rb.worldCenterOfMass : transform.position;

        public void Initialize(uint netId, Rigidbody rb)
        {
            this.netId = netId;
            _rb = rb;
        }

        public void GameTick(float dt) { }

        public void OnUse(IEventCollectable ec)
        {
            logger.Log("Use action triggered", this);
            if (isAttacking)
                return;
            isAttacking = true;
            StartCoroutine(resetAttackCooldown());

            ec.Collect(new AttackEvent(netId, new AttackEventContent { AttackType = 0 }));

            var currentHitPoint = HitPoint;
            logger.Log(
                $"[UseSystem] Attack from netId={netId} | hitPoint={currentHitPoint} | radius={hitRadius} | layerMask={targetLayer.value}",
                this
            );

            Collider[] hitTargets = Physics.OverlapSphere(currentHitPoint, hitRadius, targetLayer);
            foreach (Collider target in hitTargets)
            {
                var targetNetId = target.GetComponent<NetworkIdentity>()?.netId;
                if (targetNetId.HasValue && targetNetId.Value == netId)
                {
                    continue;
                }
                var healthSystem = target.transform.root.GetComponentInChildren<HealthSystem>();

                healthSystem.TakeDamage(attackDamage);
                if (targetNetId.HasValue)
                {
                    ec.Collect(
                        new HitEvent(
                            targetNetId.Value,
                            healthSystem.CurrentHealth,
                            healthSystem.MaxHealth
                        )
                    );
                }
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
            Gizmos.DrawWireSphere(HitPoint, hitRadius);
        }
    }
}
