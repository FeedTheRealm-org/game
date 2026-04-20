using FTR.Gameplay.Server.Characters.Systems.UseSystemComplements;
using FTR.Gameplay.Server.Utils.UseEquipment;

namespace FTR.Gameplay.Server.Characters.Systems.UseSystemComplements.UseStrategies
{
    /// <summary>
    /// Fallback strategy used when the active slot has no item.
    /// Uses the config defaults for damage, range and cooldown.
    /// </summary>
    public sealed class BareHandsStrategy : IUseStrategy
    {
        public float GetCooldown(UseContext ctx) => ctx.Config.AttackCooldown;

        public void RecordCooldown(UseContext ctx, SlotCooldownTracker cooldowns, int activeSlot) =>
            cooldowns.RecordSlotUsed(activeSlot, ctx.Config.AttackCooldown);

        public void Execute(UseContext ctx) =>
            MeleeWeaponStrategy.PerformMeleeAttack(
                ctx,
                ctx.Config.UnequippedDamage + ctx.StatMods.FlatDamageBonus,
                ctx.Config.UnequippedRange
            );
    }
}
