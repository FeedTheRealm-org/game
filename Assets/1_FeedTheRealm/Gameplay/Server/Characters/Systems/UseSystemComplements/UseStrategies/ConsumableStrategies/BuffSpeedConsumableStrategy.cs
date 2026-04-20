using FTR.Gameplay.Server.Characters.Systems;
using FTR.Gameplay.Server.Characters.Systems.UseSystemComplements;
using FTR.Gameplay.Server.Utils.UseEquipment;
using FTRShared.Runtime.Models;

namespace FTR.Gameplay.Server.Characters.Systems.UseSystemComplements.UseStrategies
{
    /// <summary>
    /// Applies a temporary speed buff to the player when consumed.
    /// </summary>
    public sealed class BuffSpeedConsumableStrategy : ConsumableStrategyBase
    {
        public BuffSpeedConsumableStrategy(ConsumableItemData data)
            : base(data) { }

        protected override void ExecuteConsumable(UseContext ctx)
        {
            ctx.Logger?.Log(
                $"[BuffSpeedConsumable] Player:{ctx.NetId} speed buff +{_data.value} for {_data.duration}s."
            );
            ctx.Movement?.ApplySpeedBuff(_data.value, _data.duration);
        }
    }
}
