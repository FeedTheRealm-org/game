using FTR.Core.Common.Protocol.RpcMessages;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Characters.Systems;
using FTR.Gameplay.Server.Characters.Systems.UseSystemComplements;
using FTR.Gameplay.Server.Utils.UseEquipment;
using FTRShared.Runtime.Models;
using Mirror;
using UnityEngine;

namespace FTR.Gameplay.Server.Characters.Systems.UseSystemComplements.UseStrategies
{
    public sealed class RangedWeaponStrategy : IUseStrategy
    {
        private readonly WeaponItemData _data;
        private int _currentAmmo;

        public RangedWeaponStrategy(WeaponItemData data)
        {
            _data = data;
            _currentAmmo = data.ammo;
        }

        public float GetCooldown(UseContext ctx)
        {
            return _currentAmmo > 0 ? _data.attackSpeed : _data.reloadSpeed;
        }

        public void RecordCooldown(UseContext ctx, SlotCooldownTracker cooldowns, int activeSlot)
        {
            _currentAmmo--;

            if (_currentAmmo > 0)
            {
                cooldowns.RecordSlotUsed(activeSlot, _data.attackSpeed);
            }
            else
            {
                cooldowns.RecordSlotUsed(activeSlot, _data.reloadSpeed);
                _currentAmmo = _data.ammo; // Reload
            }
        }

        public void Execute(UseContext ctx)
        {
            PerformRangedAttack(ctx, _data.damage + ctx.StatMods.FlatDamageBonus, _data.range);
        }

        internal static void PerformRangedAttack(UseContext ctx, int damage, float range)
        {
            var hit = Physics.Raycast(
                ctx.HitPoint,
                ctx.Direction,
                out RaycastHit raycastHit,
                range,
                ctx.TargetLayer
            );

            if (hit)
            {
                var target = raycastHit.collider;
                var targetNetId = target.GetComponent<NetworkIdentity>()?.netId;

                if (!targetNetId.HasValue || targetNetId.Value != ctx.NetId)
                {
                    var healthSystem = target.transform.root.GetComponentInChildren<HealthSystem>();

                    if (healthSystem != null)
                    {
                        var (killed, enemyTypeId) = healthSystem.TakeDamage(damage, ctx.NetId);

                        if (killed && !string.IsNullOrEmpty(enemyTypeId))
                        {
                            ctx.EnemySlayedEvent?.Raise((ctx.NetId, enemyTypeId));
                        }
                    }
                }
            }

            ctx.World?.Events.Enqueue(
                new AttackEvent(ctx.NetId, new AttackEventContent { AttackType = 1 })
            );
        }
    }
}
