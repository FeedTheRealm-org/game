using FTRShared.Runtime.Models;

namespace FTR.Gameplay.Server.Characters.Systems.UseSystemComplements.UseStrategies
{
    public sealed class HealConsumableStrategy : ConsumableStrategyBase
    {
        public HealConsumableStrategy(ConsumableItemData data)
            : base(data) { }

        protected override void ExecuteConsumable(UseContext ctx)
        {
            ctx.Logger?.Log($"[HealConsumable] Player:{ctx.NetId} healing {_data.value}.");
            ctx.Health?.Heal(_data.value);
        }
    }
}
