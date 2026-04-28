using FTR.Gameplay.Server.Characters.Systems.UseSystemComplements;
using FTR.Gameplay.Server.Utils.UseEquipment;
using FTRShared.Runtime.Models;

namespace FTR.Gameplay.Server.Characters.Systems.UseSystemComplements.UseStrategies
{
    public abstract class ConsumableStrategyBase : IUseStrategy
    {
        protected readonly ConsumableItemData _data;

        protected ConsumableStrategyBase(ConsumableItemData data) => _data = data;

        public virtual float GetCooldown(UseContext ctx) => _data.cooldown;

        public virtual bool CanExecute(
            UseContext ctx,
            SlotCooldownTracker cooldowns,
            out float remaining
        ) => CheckConsumableCooldown(ctx, cooldowns, out remaining);

        public virtual void RecordCooldown(
            UseContext ctx,
            SlotCooldownTracker cooldowns,
            int activeSlot
        ) => cooldowns.RecordConsumableUsed(_data.id, _data.cooldown);

        public void Execute(UseContext ctx)
        {
            ExecuteConsumable(ctx);
            ctx.Inventory?.ConsumeItem(_data.id);
        }

        protected virtual bool CheckConsumableCooldown(
            UseContext ctx,
            SlotCooldownTracker cooldowns,
            out float remaining
        )
        {
            if (cooldowns.IsConsumableCoolingDown(_data.id, out remaining))
            {
                ctx.Logger?.Log(
                    $"[UseSystem] Player:{ctx.NetId} consumable '{_data.id}' on cooldown ({remaining:F2}s remaining)."
                );
                return false;
            }
            return true;
        }

        protected abstract void ExecuteConsumable(UseContext ctx);
    }
}
