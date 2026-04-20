using FTR.Core.Server.Enums;
using FTR.Gameplay.Server.Utils.UseEquipment;
using FTRShared.Runtime.Models;

namespace FTR.Gameplay.Server.Characters.Systems.UseSystemComplements.UseStrategies
{
    /// <summary>
    /// Builds the correct IUseStrategy for a consumable based on its EffectType.
    /// Called from EquippedItemFactory.
    /// </summary>
    public static class ConsumableStrategyFactory
    {
        public static IUseStrategy Build(ConsumableItemData data)
        {
            return data.effectType switch
            {
                EffectType.Heal => new HealConsumableStrategy(data),
                EffectType.Buff => new BuffSpeedConsumableStrategy(data),
                EffectType.Damage => new BuffDamageConsumableStrategy(data),
                EffectType.Debuff => new NoOpConsumableStrategy(data),
                EffectType.RestoreMana => new NoOpConsumableStrategy(data),
                EffectType.DrainMana => new NoOpConsumableStrategy(data),
                EffectType.None => new NoOpConsumableStrategy(data),
                _ => new NoOpConsumableStrategy(data),
            };
        }
    }
}
