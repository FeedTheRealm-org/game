using System.Collections;
using FTR.Core.Common.Config;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.Utils;
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
        private Logging.Logger logger;

        [Inject]
        private WorldMonitor world;

        private LayerMask targetLayer;

        private bool isAttacking = false;

        private Rigidbody _rb;
        private uint netId;

        private CharacterStateStorage stateStorage;

        private Vector3 HitPoint => _rb != null ? _rb.worldCenterOfMass : transform.position;

        // AutoAttack-driven usage
        private int amountOfPlayersInRange = 0;
        private PlayerTriggerArea _attackTriggerArea;
        private Coroutine _autoAttackCoroutine;

        private bool isDead = false;

        public void Initialize(
            uint netId,
            Rigidbody rb,
            LayerMask targetLayer,
            CharacterStateStorage stateStorage
        )
        {
            this.netId = netId;
            _rb = rb;
            this.targetLayer = targetLayer;
            this.stateStorage = stateStorage;

            this.stateStorage.OnDeath += HandleDeath;
            this.stateStorage.OnRespawn += HandleRespawn;
        }

        private void HandleDeath()
        {
            isDead = true;
            isAttacking = false;
            if (_autoAttackCoroutine != null)
            {
                StopCoroutine(_autoAttackCoroutine);
                _autoAttackCoroutine = null;
            }
        }

        private void HandleRespawn()
        {
            isDead = false;
        }

        public void SetAttackTriggerArea(PlayerTriggerArea attackTriggerArea)
        {
            _attackTriggerArea = attackTriggerArea;
            _attackTriggerArea.OnPlayerEnter += StartAutoAttacking;
            _attackTriggerArea.OnPlayerExit += PlayerLeftAutoAttackRange;
        }

        private void OnDestroy()
        {
            if (_attackTriggerArea != null)
            {
                _attackTriggerArea.OnPlayerEnter -= StartAutoAttacking;
                _attackTriggerArea.OnPlayerExit -= PlayerLeftAutoAttackRange;
            }

            if (stateStorage != null)
            {
                stateStorage.OnDeath -= HandleDeath;
                stateStorage.OnRespawn -= HandleRespawn;
            }
        }

        public void GameTick(float dt) { }

        public void OnUse(IEventCollectable ec)
        {
            logger.Log("Use action triggered", this);
            if (isAttacking || isDead)
                return;
            isAttacking = true;
            StartCoroutine(resetAttackCooldown());

            Attack();
        }

        private void Attack()
        {
            if (isDead)
                return; // Cant attack while dying

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
                if (healthSystem == null)
                {
                    continue;
                }

                healthSystem.TakeDamage(attackDamage);
            }

            if (hitTargets.Length == 0)
            {
                logger.Log("No targets hit", this);
            }

            world.Events.Enqueue(new AttackEvent(netId, new AttackEventContent { AttackType = 0 }));
        }

        public void StartAutoAttacking(Collider _)
        {
            logger.Log("Target entered auto attack range", this);
            amountOfPlayersInRange++;
            if (_autoAttackCoroutine == null)
            {
                _autoAttackCoroutine = StartCoroutine(KeepAutoAttacking());
            }
        }

        public void PlayerLeftAutoAttackRange(Collider _)
        {
            amountOfPlayersInRange = Mathf.Max(0, amountOfPlayersInRange - 1);
            if (amountOfPlayersInRange == 0 && _autoAttackCoroutine != null)
            {
                StopCoroutine(_autoAttackCoroutine);
                _autoAttackCoroutine = null;
            }
        }

        private IEnumerator KeepAutoAttacking()
        {
            while (amountOfPlayersInRange > 0)
            {
                Attack();
                yield return new WaitForSeconds(attackCooldown * 2);
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
