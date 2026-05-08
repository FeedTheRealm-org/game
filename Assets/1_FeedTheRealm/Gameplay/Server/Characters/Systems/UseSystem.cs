using System;
using System.Collections;
using System.Collections.Generic;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.Utils;
using FTR.Gameplay.Server.Characters.Systems.UseSystemComplements;
using FTR.Gameplay.Server.Characters.Systems.UseSystemComplements.UseStrategies;
using FTR.Gameplay.Server.Utils.UseEquipment;
using FTRShared.Runtime.Models;
using UnityEngine;
using VContainer;

namespace FTR.Gameplay.Server.Characters.Systems
{
    public class UseSystem : MonoBehaviour, IGameTickable
    {
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private ServerConfig config;

        [Inject]
        private WorldMonitor world;

        private EnemySlayedEvent enemySlayedEvent;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            enemySlayedEvent = resolver.Resolve<EnemySlayedEvent>();
        }

        private LayerMask targetLayer;
        private Rigidbody _rb;
        private uint netId;
        private CharacterStateStorage stateStorage;

        private bool isDead = false;
        private int activeSlot = 0;

        private EquippedItem currentEquipped;
        private IUseStrategy currentStrategy;
        private SlotCooldownTracker cooldowns;
        private UseContext ctx;

        public event Action<float> OnAttackRangeChanged;
        public float CurrentAttackRange { get; private set; }

        private Vector3 HitPoint => _rb != null ? _rb.worldCenterOfMass : transform.position;

        private HashSet<Collider> _playersInRange = new HashSet<Collider>();
        private PlayerTriggerArea _attackTriggerArea;
        private Coroutine _autoAttackCoroutine;

        // ── Initialization ────────────────────────────────────────────────────

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

            cooldowns = new SlotCooldownTracker(config.FastSlotSize);
            currentStrategy = new BareHandsStrategy();
            CurrentAttackRange = config.UnequippedRange;

            var movement = GetComponentInParent<MovementSystem>();
            var health = GetComponentInParent<HealthSystem>();
            var inventory = transform.root.GetComponentInChildren<InventorySystem>();

            ctx = new UseContext(
                netId: netId,
                hitPointProvider: () => HitPoint,
                targetLayerProvider: () => this.targetLayer,
                config: config,
                movement: movement,
                health: health,
                inventory: inventory,
                statMods: new StatModifierBag(),
                enemySlayedEvent: enemySlayedEvent,
                world: world,
                logger: logger,
                logSource: this
            );

            stateStorage.OnDeath += HandleDeath;
            stateStorage.OnRespawn += HandleRespawn;
        }

        public void SetAttackTriggerArea(PlayerTriggerArea attackTriggerArea)
        {
            _attackTriggerArea = attackTriggerArea;
            _attackTriggerArea.Initialize(CurrentAttackRange);
            _attackTriggerArea.OnPlayerEnter += StartAutoAttacking;
            _attackTriggerArea.OnPlayerExit += PlayerLeftAutoAttackRange;

            OnAttackRangeChanged -= UpdateAttackTriggerArea;
            OnAttackRangeChanged += UpdateAttackTriggerArea;
        }

        public void SetStrategy(IUseStrategy strategy)
        {
            currentStrategy = strategy ?? new BareHandsStrategy();
        }

        private void UpdateAttackTriggerArea(float attackRange)
        {
            CurrentAttackRange = attackRange;

            if (_attackTriggerArea != null)
                _attackTriggerArea.Initialize(attackRange);
        }

        private void HandleDeath()
        {
            isDead = true;
            if (_autoAttackCoroutine != null)
            {
                StopCoroutine(_autoAttackCoroutine);
                _autoAttackCoroutine = null;
            }
        }

        private void HandleRespawn() => isDead = false;

        private void OnDestroy()
        {
            OnAttackRangeChanged -= UpdateAttackTriggerArea;

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

        public void OnUse(IEventCollectable ec, Vector3 direction)
        {
            if (isDead)
                return;

            if (!cooldowns.IsSlotReady(activeSlot, out float slotRemaining))
            {
                logger?.Log(
                    $"[UseSystem] Player:{netId} slot {activeSlot} on cooldown ({slotRemaining:F2}s remaining).",
                    this
                );
                return;
            }

            ctx.Direction = direction;

            if (!currentStrategy.CanExecute(ctx, cooldowns, out float strategyRemaining))
                return;

            currentStrategy.RecordCooldown(ctx, cooldowns, activeSlot);
            currentStrategy.Execute(ctx);
        }

        public void StartAutoAttacking(Collider playerCollider)
        {
            if (playerCollider != null)
                _playersInRange.Add(playerCollider);

            if (_autoAttackCoroutine == null)
                _autoAttackCoroutine = StartCoroutine(KeepAutoAttacking());
        }

        public void PlayerLeftAutoAttackRange(Collider playerCollider)
        {
            if (playerCollider != null)
                _playersInRange.Remove(playerCollider);

            if (_playersInRange.Count == 0 && _autoAttackCoroutine != null)
            {
                StopCoroutine(_autoAttackCoroutine);
                _autoAttackCoroutine = null;
            }
        }

        private IEnumerator KeepAutoAttacking()
        {
            while (_playersInRange.Count > 0)
            {
                _playersInRange.RemoveWhere(c => c == null || !c.gameObject.activeInHierarchy);

                if (_playersInRange.Count == 0)
                    break;

                Collider target = null;
                foreach (var p in _playersInRange)
                {
                    target = p;
                    break;
                }

                if (target != null)
                {
                    Vector3 dir = (target.transform.position - transform.position).normalized;
                    dir.y = 0; // Typically we attack horizontally
                    ctx.Direction = dir == Vector3.zero ? transform.forward : dir.normalized;

                    if (currentStrategy.CanExecute(ctx, cooldowns, out float strategyRemaining))
                    {
                        currentStrategy.RecordCooldown(ctx, cooldowns, activeSlot);
                        currentStrategy.Execute(ctx);
                    }
                }

                yield return new WaitForSeconds(currentStrategy.GetCooldown(ctx));
            }

            _autoAttackCoroutine = null;
        }

        public void EquipItem((string itemId, int slotIndex) data)
        {
            activeSlot = data.slotIndex;

            var result = EquippedItemFactory.Build(data.itemId, config, logger, this);
            currentEquipped = result.Item;
            currentStrategy = result.Strategy;
            CurrentAttackRange = currentEquipped switch
            {
                WeaponEquipped weapon => weapon.Data.range,
                ConsumableEquipped => config.UseRange,
                _ => config.UnequippedRange,
            };

            OnAttackRangeChanged?.Invoke(CurrentAttackRange);

            logger?.Log(
                $"[UseSystem] Player:{netId} equipped '{data.itemId}' in slot {data.slotIndex}.",
                this
            );
        }

        private void OnDrawGizmos()
        {
            if (config == null)
                return;

            if (currentEquipped == null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(HitPoint, config.UnequippedRange);
                return;
            }

            if (currentEquipped is WeaponEquipped weapon)
            {
                if (weapon.Data.weaponType == WeaponType.Melee)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(HitPoint, weapon.Data.range);
                }
                else if (weapon.Data.weaponType == WeaponType.Ranged)
                {
                    Gizmos.color = Color.red;
                    Vector3 dir =
                        ctx != null && ctx.Direction != Vector3.zero
                            ? ctx.Direction
                            : transform.forward;
                    Gizmos.DrawRay(HitPoint, dir.normalized * weapon.Data.range);
                }
            }
        }
    }
}
