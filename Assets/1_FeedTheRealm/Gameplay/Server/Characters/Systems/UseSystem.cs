using System.Collections;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.Utils;
using FTR.Gameplay.Server.Utils.UseEquipment;
using FTRShared.Runtime.Models;
using Mirror;
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

        private ItemEquippedEvent itemEquippedEvent;
        private EnemySlayedEvent enemySlayedEvent;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            itemEquippedEvent = resolver.Resolve<ItemEquippedEvent>();
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
        private UseContext _context;

        private Vector3 HitPoint => _rb != null ? _rb.worldCenterOfMass : transform.position;

        private int amountOfPlayersInRange = 0;
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

            cooldowns = new SlotCooldownTracker(slotCount: 5);
            currentStrategy = new BareHandsStrategy();

            var movement = GetComponentInParent<MovementSystem>();
            var health = GetComponentInParent<HealthSystem>();
            var inventory = GetComponentInParent<InventorySystem>();

            _context = new UseContext(
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
            SubscribeToItemEquipped();
        }

        public void SetAttackTriggerArea(PlayerTriggerArea attackTriggerArea)
        {
            _attackTriggerArea = attackTriggerArea;
            _attackTriggerArea.OnPlayerEnter += StartAutoAttacking;
            _attackTriggerArea.OnPlayerExit += PlayerLeftAutoAttackRange;
        }

        public void SetStrategy(IUseStrategy strategy)
        {
            currentStrategy = strategy ?? new BareHandsStrategy();
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

            var ctx = _context;

            if (!currentStrategy.CanExecute(ctx, cooldowns, out float strategyRemaining))
                return;

            currentStrategy.RecordCooldown(ctx, cooldowns, activeSlot);
            currentStrategy.Execute(ctx);
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
                currentStrategy.Execute(_context);
                yield return new WaitForSeconds(currentStrategy.GetCooldown(_context) * 2);
            }
        }

        private void OnItemEquipped((uint playerNetId, string itemId, int slotIndex) data)
        {
            if (data.playerNetId != netId)
                return;

            activeSlot = data.slotIndex;

            var result = EquippedItemFactory.Build(data.itemId, config, logger, this);
            currentEquipped = result.Item;
            currentStrategy = result.Strategy;

            logger?.Log(
                $"[UseSystem] Player:{netId} equipped '{data.itemId}' in slot {data.slotIndex}.",
                this
            );
        }

        private void SubscribeToItemEquipped()
        {
            if (itemEquippedEvent != null)
                itemEquippedEvent.OnRaised += OnItemEquipped;
        }

        private void UnsubscribeFromItemEquipped()
        {
            if (itemEquippedEvent != null)
                itemEquippedEvent.OnRaised -= OnItemEquipped;
        }

        private void OnDrawGizmos()
        {
            if (config == null)
                return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(HitPoint, config.UnequippedRange);
        }
    }
}
