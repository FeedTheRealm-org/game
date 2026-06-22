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
            Vector3 perpendicular = Vector3.Cross(ctx.Direction, Vector3.up).normalized;
            float halfSpacing = ctx.Config.RangedWeaponRaySpacing / 2f;

            // Three parallel raycasts: center, left, right
            Vector3[] rayPositions = new Vector3[]
            {
                ctx.HitPoint,
                ctx.HitPoint - perpendicular * halfSpacing,
                ctx.HitPoint + perpendicular * halfSpacing,
            };

            LayerMask shootMask = ctx.TargetLayer | ctx.Config.GroundLayer | ctx.Config.SlopeLayer;

            Collider hitTarget = null;
            RaycastHit firstHit = new RaycastHit();
            float closestDistance = float.MaxValue;

            foreach (var rayPos in rayPositions)
            {
                if (
                    Physics.Raycast(
                        rayPos,
                        ctx.Direction,
                        out RaycastHit raycastHit,
                        range,
                        shootMask
                    )
                )
                {
                    // First thing hit is a wall/ground/slope → this ray is blocked.
                    if (!IsOnLayer(raycastHit.collider.gameObject, ctx.TargetLayer))
                        continue;

                    if (raycastHit.distance < closestDistance)
                    {
                        closestDistance = raycastHit.distance;
                        firstHit = raycastHit;
                        hitTarget = raycastHit.collider;
                    }
                }
            }

            if (hitTarget != null)
            {
                var targetNetId = hitTarget.GetComponent<NetworkIdentity>()?.netId;

                if (!targetNetId.HasValue || targetNetId.Value != ctx.NetId)
                {
                    var healthSystem =
                        hitTarget.transform.root.GetComponentInChildren<HealthSystem>();

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

        private static bool IsOnLayer(GameObject go, LayerMask mask)
        {
            return (mask.value & (1 << go.layer)) != 0;
        }
    }
}
