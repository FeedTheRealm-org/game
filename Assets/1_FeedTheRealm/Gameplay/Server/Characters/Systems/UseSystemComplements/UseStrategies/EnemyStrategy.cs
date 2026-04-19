using FTR.Core.Server.Enums;
using FTR.Gameplay.Server.Characters.Systems.UseSystemComplements;
using FTR.Gameplay.Server.Utils.UseEquipment;

namespace FTR.Gameplay.Server.Characters.Systems.UseSystemComplements.UseStrategies
{
    public sealed class EnemyMeleeStrategy : IUseStrategy
    {
        private readonly EnemyData _data;

        public EnemyMeleeStrategy(EnemyData data) => _data = data;

        public float GetCooldown(UseContext ctx) => _data.speed;

        // For now works the same as recording a weapon cooldown, since enemies
        // don't have an inventory (but CDTracker use slot 0 always and it's decoupled
        // from inventory). Can be improved later if needed,
        // e.g. by adding a separate cooldown tracker for enemies in UseSystem.
        public void RecordCooldown(UseContext ctx, SlotCooldownTracker cooldowns, int activeSlot) =>
            cooldowns.RecordSlotUsed(activeSlot, _data.speed);

        // For now, enemies use the same melee attack logic, if ranged attacks
        // are implemented in the future this can be extended with a separate
        // PerformRangedAttack method in a new strategy for each type of enemy.
        public void Execute(UseContext ctx) =>
            MeleeWeaponStrategy.PerformMeleeAttack(ctx, _data.damage, _data.range);
    }
}
