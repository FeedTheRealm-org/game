using FTR.Core.Server.Enums;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Events;
using FTR.Gameplay.Server.Characters.Systems.UseSystemComplements;
using FTR.Gameplay.Server.Utils.UseEquipment;

namespace FTR.Gameplay.Server.Characters.Systems.UseSystemComplements.UseStrategies
{
    public sealed class ConsumableStrategy : IUseStrategy
    {
        private readonly ConsumableItemData _data;

        public ConsumableStrategy(ConsumableItemData data) => _data = data;

        public float GetCooldown(UseContext ctx) => _data.cooldown;

        public bool CanExecute(UseContext ctx, SlotCooldownTracker cooldowns, out float remaining)
        {
            if (cooldowns.IsConsumableCoolingDown(_data.id, out remaining))
            {
                ctx.Logger?.Log(
                    $"[UseSystem] Player:{ctx.NetId} consumable '{_data.id}' on cooldown ({remaining:F2}s remaining).",
                    ctx.LogSource
                );
                return false;
            }
            return true;
        }

        public void RecordCooldown(UseContext ctx, SlotCooldownTracker cooldowns, int activeSlot)
        {
            cooldowns.RecordConsumableUsed(_data.id, _data.cooldown);
            // Uncomment to also lock the slot when consuming:
            // cooldowns.RecordSlotUsed(activeSlot, _data.cooldown);
        }

        public void Execute(UseContext ctx)
        {
            ctx.Logger?.Log(
                $"[UseSystem] Player:{ctx.NetId} consuming '{_data.id}' (effect={_data.effectType}).",
                ctx.LogSource
            );

            switch (_data.effectType)
            {
                case EffectType.Heal:
                    ctx.PlayerHealEvent?.Raise((ctx.NetId, _data.value));
                    break;
                case EffectType.Buff:
                    ctx.PlayerBuffSpeedEvent?.Raise((ctx.NetId, _data.value, _data.duration));
                    break;
                case EffectType.Damage:
                    // TODO: bonus damage stat for duration
                    break;
                case EffectType.RestoreMana:
                case EffectType.DrainMana:
                case EffectType.Debuff:
                case EffectType.None:
                    ctx.Logger?.Log(
                        $"[UseSystem] Consuming effect '{_data.effectType}' - no logic yet.",
                        ctx.LogSource
                    );
                    break;
            }

            ctx.ConsumeItemEvent?.Raise((ctx.NetId, _data.id));
        }
    }
}
