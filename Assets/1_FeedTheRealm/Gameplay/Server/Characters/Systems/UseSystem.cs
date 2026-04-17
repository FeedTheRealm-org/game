using System.Collections;
using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Common.Utils;
using FTR.Core.Server.Config;
using FTR.Core.Server.Enums;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Common.NetworkEntities.Characters;
using FTR.Gameplay.Common.Utils;
using FTR.Gameplay.Server.Registry;
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
        private float attackCooldown = 0.4f;

        [SerializeField]
        private int attackDamage = 40;

        [SerializeField]
        private float hitRadius;

        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private ServerConfig config;

        [Inject]
        private WorldMonitor world;

        private ItemEquippedEvent itemEquippedEvent;
        private EnemySlayedEvent enemySlayedEvent;
        private ConsumeItemEvent consumeItemEvent;
        private PlayerHealEvent playerHealEvent;
        private PlayerBuffSpeedEvent playerBuffSpeedEvent;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            itemEquippedEvent = resolver.Resolve<ItemEquippedEvent>();
            consumeItemEvent = resolver.Resolve<ConsumeItemEvent>();
            playerHealEvent = resolver.Resolve<PlayerHealEvent>();
            playerBuffSpeedEvent = resolver.Resolve<PlayerBuffSpeedEvent>();
            enemySlayedEvent = resolver.Resolve<EnemySlayedEvent>();
        }

        // ── Runtime state ─────────────────────────────────────────────────────

        private LayerMask targetLayer;
        private Rigidbody _rb;
        private uint netId;
        private CharacterStateStorage stateStorage;

        private bool isDead = false;
        private int activeSlot = 0;
        private EquippedItem currentEquipped;
        private SlotCooldownTracker cooldowns;

        private Vector3 HitPoint => _rb != null ? _rb.worldCenterOfMass : transform.position;

        private int amountOfPlayersInRange = 0;
        private PlayerTriggerArea _attackTriggerArea;
        private Coroutine _autoAttackCoroutine;

        public void SetAttackDamage(int damage) => attackDamage = damage;

        public void SetRange(float radius) => hitRadius = radius;

        public void SetAttackCooldown(float cd) => attackCooldown = cd;

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

            switch (currentEquipped)
            {
                case ConsumableEquipped consumable:
                    if (cooldowns.IsConsumableCoolingDown(consumable.Data.id, out float cdLeft))
                    {
                        logger?.Log(
                            $"[UseSystem] Player:{netId} consumable '{consumable.Data.id}' on cooldown ({cdLeft:F2}s remaining).",
                            this
                        );
                        return;
                    }
                    //cooldowns.RecordSlotUsed(activeSlot, consumable.Data.cooldown); uncomment if you want the slot to also be on cooldown when using a consumable
                    cooldowns.RecordConsumableUsed(consumable.Data.id, consumable.Data.cooldown);
                    Consume(consumable.Data);
                    break;

                case WeaponEquipped weapon:
                    cooldowns.RecordSlotUsed(activeSlot, weapon.Data.attackSpeed);
                    Attack();
                    break;

                default:
                    cooldowns.RecordSlotUsed(activeSlot, config.AttackCooldown);
                    Attack();
                    break;
            }
        }

        private void Consume(ConsumableItemData data)
        {
            if (isDead)
                return;

            logger.Log(
                $"[UseSystem] Player:{netId} consuming '{data.id}' (effect={data.effectType}).",
                this
            );

            switch (data.effectType)
            {
                case EffectType.Heal:
                    playerHealEvent?.Raise((netId, data.value));
                    break;
                case EffectType.Buff:
                    playerBuffSpeedEvent?.Raise((netId, data.value, data.duration));
                    break;
                case EffectType.Damage:
                    // TODO: bonus damage stat for duration
                    break;
                case EffectType.RestoreMana:
                case EffectType.DrainMana:
                case EffectType.Debuff:
                case EffectType.None:
                    logger.Log(
                        $"[UseSystem] Consuming effect '{data.effectType}' - no logic yet.",
                        this
                    );
                    break;
            }

            consumeItemEvent?.Raise((netId, data.id));
        }

        private void Attack()
        {
            if (isDead)
                return;

            Collider[] hitTargets = Physics.OverlapSphere(HitPoint, hitRadius, targetLayer);

            foreach (Collider target in hitTargets)
            {
                var targetNetId = target.GetComponent<NetworkIdentity>()?.netId;
                if (targetNetId.HasValue && targetNetId.Value == netId)
                    continue;

                var healthSystem = target.transform.root.GetComponentInChildren<HealthSystem>();
                if (healthSystem == null)
                    continue;

                var (killed, enemyTypeId) = healthSystem.TakeDamage(attackDamage, netId);

                if (killed && !string.IsNullOrEmpty(enemyTypeId))
                {
                    enemySlayedEvent.Raise((netId, enemyTypeId));
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

        private void OnItemEquipped((uint playerNetId, string itemId, int slotIndex) data)
        {
            if (data.playerNetId != netId)
                return;

            activeSlot = data.slotIndex;
            currentEquipped = BuildEquippedItem(data.itemId);

            logger?.Log(
                $"[UseSystem] Player:{netId} equipped '{data.itemId}' in slot {data.slotIndex}.",
                this
            );
        }

        private EquippedItem BuildEquippedItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                SetAttackDamage(config.UnequippedDamage);
                SetRange(config.UnequippedRange);
                SetAttackCooldown(config.AttackCooldown);
                logger?.Log(
                    $"[UseSystem] No item equipped. Using default attack: dmg={config.UnequippedDamage} range={config.UnequippedRange} speed={config.AttackCooldown}.",
                    this
                );
                return null;
            }

            switch (ServerItemsRegistry.GetItemTypeById(itemId))
            {
                case EquipmentType.Weapon:
                    var weaponData = ServerItemsRegistry.GetWeaponById(itemId);
                    if (weaponData == null)
                    {
                        logger?.Log(
                            $"[UseSystem] No weapon data for '{itemId}'.",
                            this,
                            Logging.LogType.Warning
                        );
                        return null;
                    }
                    SetAttackDamage(weaponData.damage);
                    SetRange(weaponData.range);
                    SetAttackCooldown(weaponData.attackSpeed);
                    logger?.Log(
                        $"[UseSystem] Weapon '{itemId}': dmg={weaponData.damage} range={weaponData.range} speed={weaponData.attackSpeed}.",
                        this
                    );
                    return new WeaponEquipped(weaponData);

                case EquipmentType.Consumable:
                    var consumableData = ServerItemsRegistry.GetConsumableById(itemId);
                    if (consumableData == null)
                    {
                        logger?.Log(
                            $"[UseSystem] No consumable data for '{itemId}'.",
                            this,
                            Logging.LogType.Warning
                        );
                        return null;
                    }
                    logger?.Log(
                        $"[UseSystem] Consumable '{itemId}': effect={consumableData.effectType} value={consumableData.value} cd={consumableData.cooldown}.",
                        this
                    );
                    return new ConsumableEquipped(consumableData);

                default:
                    logger?.Log(
                        $"[UseSystem] Unknown item type for '{itemId}'.",
                        this,
                        Logging.LogType.Warning
                    );
                    return null;
            }
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
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(HitPoint, hitRadius);
        }
    }
}
