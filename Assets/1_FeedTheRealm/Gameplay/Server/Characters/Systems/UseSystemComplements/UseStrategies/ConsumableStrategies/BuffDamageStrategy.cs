using FTR.Gameplay.Server.Characters.Systems.UseSystemComplements;
using FTR.Gameplay.Server.Utils.UseEquipment;
using FTRShared.Runtime.Models;

namespace FTR.Gameplay.Server.Characters.Systems.UseSystemComplements.UseStrategies
{
    /// <summary>
    /// Applies a temporary flat damage bonus to the player when consumed.
    /// </summary>
    public sealed class BuffDamageConsumableStrategy : ConsumableStrategyBase
    {
        public BuffDamageConsumableStrategy(ConsumableItemData data)
            : base(data) { }

        protected override void ExecuteConsumable(UseContext ctx)
        {
            ctx.Logger?.Log(
                $"[BuffDamageConsumable] Player:{ctx.NetId} damage buff +{_data.value} for {_data.duration}s.",
                ctx.LogSource
            );
            ctx.StatMods.ApplyFlatDamage(_data.value, _data.duration);
        }
    }
}
