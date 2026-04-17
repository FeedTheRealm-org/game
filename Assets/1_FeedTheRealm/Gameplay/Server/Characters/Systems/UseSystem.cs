using System.Collections;
using FTR.Core.Common.Config;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Enums;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.Utils;
using FTR.Gameplay.Server.Registry;
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

        [Inject]
        private ItemEquippedEvent itemEquippedEvent;

        [Inject]
        private EnemySlayedEvent enemySlayedEvent;

        private LayerMask targetLayer;
        private bool isAttacking = false;
        private Rigidbody _rb;
        private uint netId;
        private CharacterStateStorage stateStorage;

        private Vector3 HitPoint => _rb != null ? _rb.worldCenterOfMass : transform.position;

        private int amountOfPlayersInRange = 0;
        private PlayerTriggerArea _attackTriggerArea;
        private Coroutine _autoAttackCoroutine;
        private bool isDead = false;

        // Could be a config function instead of each one but enemies have other stats
        // could be changed on future if they can equip items or have buffs/debuffs that change their stats
        public void SetAttackDamage(int damage)
        {
            attackDamage = damage;
        }

        public void SetRange(float radius)
        {
            hitRadius = radius;
        }

        public void SetAttackCooldown(float cooldown)
        {
            attackCooldown = cooldown;
        }

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
            SubscribeToItemEquipped();
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

        private void HandleRespawn() => isDead = false;

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
            UnsubscribeFromItemEquipped();
        }

        public void GameTick(float dt) { }

        public void OnUse(IEventCollectable ec)
        {
            //logger.Log("Use action triggered", this);
            if (isAttacking || isDead)
                return;
            isAttacking = true;
            StartCoroutine(resetAttackCooldown());
            Attack();
        }

        private void Attack()
        {
            if (isDead)
                return;

            var currentHitPoint = HitPoint;
            /*logger.Log(
                $"[UseSystem] Attack from netId={netId} | hitPoint={currentHitPoint} | radius={hitRadius} | layerMask={targetLayer.value}",
                this
            );*/

            Collider[] hitTargets = Physics.OverlapSphere(currentHitPoint, hitRadius, targetLayer);
            foreach (Collider target in hitTargets)
            {
                var targetNetId = target.GetComponent<NetworkIdentity>()?.netId;
                if (targetNetId.HasValue && targetNetId.Value == netId)
                    continue;

                var healthSystem = target.transform.root.GetComponentInChildren<HealthSystem>();
                if (healthSystem == null)
                    continue;

                var (killed, enemyTypeId) = healthSystem.TakeDamage(attackDamage, this.netId);

                if (killed)
                {
                    logger?.Log(
                        $"[UseSystem] Enemy {enemyTypeId} killed by {this.netId}, raising event.",
                        this
                    );

                    if (!string.IsNullOrEmpty(enemyTypeId))
                        enemySlayedEvent.Raise((this.netId, enemyTypeId));
                }
            }

            if (hitTargets.Length == 0)
                logger.Log("No targets hit", this);

            world.Events.Enqueue(new AttackEvent(netId, new AttackEventContent { AttackType = 0 }));
        }

        public void StartAutoAttacking(Collider _)
        {
            amountOfPlayersInRange++;
            if (_autoAttackCoroutine == null)
                _autoAttackCoroutine = StartCoroutine(KeepAutoAttacking());
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

        private void OnItemEquipped((uint playerNetId, string itemId) data)
        {
            if (data.playerNetId != netId)
                return;

            logger?.Log(
                $"[InventorySystem] Item equipped: Player:{netId} equipped item '{data.itemId}'.",
                this
            );
            var itemType = ServerItemsRegistry.GetItemTypeById(data.itemId);
            if (itemType == EquipmentType.None)
            {
                logger?.Log(
                    $"[UseSystem] No item type found for equipped item '{data.itemId}'.",
                    this,
                    Logging.LogType.Warning
                );
                return;
            }
            var itemData = ServerItemsRegistry.GetItemById(data.itemId);
            if (itemData == null)
            {
                logger?.Log(
                    $"[UseSystem] No item data found for equipped item '{data.itemId}'. Cannot update stats.",
                    this,
                    Logging.LogType.Warning
                );
                return;
            }
            if (itemType == EquipmentType.Weapon)
            {
                var weaponData = ServerItemsRegistry.GetWeaponById(data.itemId);
                if (weaponData != null)
                {
                    SetAttackDamage(weaponData.damage);
                    SetRange(weaponData.range);
                    SetAttackCooldown(weaponData.attackSpeed);
                    logger?.Log(
                        $"[UseSystem] Updated stats from equipped weapon '{data.itemId}': damage={weaponData.damage}, range={weaponData.range}, attackSpeed={weaponData.attackSpeed}.",
                        this
                    );
                }
            }
            else
            {
                var consumableData = ServerItemsRegistry.GetConsumableById(data.itemId);
                if (consumableData != null)
                {
                    //TODO: implement buffs/debuffs from consumables that can affect attack stats
                }
            }
        }

        private void SubscribeToItemEquipped()
        {
            if (itemEquippedEvent == null)
                return;
            itemEquippedEvent.OnRaised += OnItemEquipped;
        }

        private void UnsubscribeFromItemEquipped()
        {
            if (itemEquippedEvent == null)
                return;
            itemEquippedEvent.OnRaised -= OnItemEquipped;
        }
    }
}
