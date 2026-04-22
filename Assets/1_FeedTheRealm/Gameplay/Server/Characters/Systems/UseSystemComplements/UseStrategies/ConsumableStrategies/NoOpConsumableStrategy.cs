using FTR.Gameplay.Server.Characters.Systems.UseSystemComplements;
using FTR.Gameplay.Server.Utils.UseEquipment;
using FTRShared.Runtime.Models;

namespace FTR.Gameplay.Server.Characters.Systems.UseSystemComplements.UseStrategies
{
    public sealed class NoOpConsumableStrategy : ConsumableStrategyBase
    {
        public NoOpConsumableStrategy(ConsumableItemData data)
            : base(data) { }

        protected override void ExecuteConsumable(UseContext ctx)
        {
            ctx.Logger?.Log(
                $"[NoOpConsumable] Player:{ctx.NetId} used '{_data.id}' (effect={_data.effectType}) — no logic yet."
            );
        }
    }
}
