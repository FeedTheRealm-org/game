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
    public sealed class MeleeWeaponStrategy : IUseStrategy
    {
        private readonly WeaponItemData _data;

        public MeleeWeaponStrategy(WeaponItemData data) => _data = data;

        public float GetCooldown(UseContext ctx) => _data.attackSpeed;

        public void RecordCooldown(UseContext ctx, SlotCooldownTracker cooldowns, int activeSlot) =>
            cooldowns.RecordSlotUsed(activeSlot, _data.attackSpeed);

        public void Execute(UseContext ctx)
        {
            PerformMeleeAttack(ctx, _data.damage + ctx.StatMods.FlatDamageBonus, _data.range);
        }

        internal static void PerformMeleeAttack(UseContext ctx, int damage, float radius)
        {
            Collider[] hitTargets = Physics.OverlapSphere(ctx.HitPoint, radius, ctx.TargetLayer);

            foreach (Collider target in hitTargets)
            {
                var targetNetId = target.GetComponent<NetworkIdentity>()?.netId;

                if (targetNetId.HasValue && targetNetId.Value == ctx.NetId)
                    continue;

                var healthSystem = target.transform.root.GetComponentInChildren<HealthSystem>();

                if (healthSystem == null)
                    continue;

                var (killed, enemyTypeId) = healthSystem.TakeDamage(damage, ctx.NetId);

                if (killed && !string.IsNullOrEmpty(enemyTypeId))
                    ctx.EnemySlayedEvent?.Raise((ctx.NetId, enemyTypeId));
            }

            if (hitTargets.Length == 0)
                ctx.Logger?.Log("No targets hit");

            ctx.World?.Events.Enqueue(
                new AttackEvent(ctx.NetId, new AttackEventContent { AttackType = 0 })
            );
        }
    }
}
