using FTRShared.Runtime.Models;
using UnityEngine.UIElements;

namespace UI.ItemStats
{
    /// <summary>
    /// Encapsulates the logic to show and hide the stats labels in the tooltip.
    /// </summary>
    public class TooltipStatsPresenter
    {
        private readonly Label effectLabel;
        private readonly Label valueLabel;
        private readonly Label durationLabel;
        private readonly Label cooldownLabel;
        private readonly Label maxStackLabel;

        public TooltipStatsPresenter(
            Label effect,
            Label value,
            Label duration,
            Label cooldown,
            Label maxStack
        )
        {
            effectLabel = effect;
            valueLabel = value;
            durationLabel = duration;
            cooldownLabel = cooldown;
            maxStackLabel = maxStack;
        }

        /// <summary>
        /// Entry point for showing stats for any item type.
        /// Clears previous stats and delegates to the appropriate
        /// type-specific implementation based on the concrete item.
        /// </summary>
        public void ShowStats(ItemData item)
        {
            HideAllStats();

            switch (item)
            {
                case ConsumableItemData consumable:
                    ShowConsumableStats(consumable);
                    break;
                case WeaponItemData weapon:
                    ShowWeaponStats(weapon);
                    break;
                default:
                    // No stats for unknown item types; keep all labels hidden.
                    break;
            }
        }

        public void ShowConsumableStats(ConsumableItemData consumable)
        {
            if (effectLabel != null)
            {
                effectLabel.text = $"Effect: {consumable.effectType}";
                effectLabel.style.display = DisplayStyle.Flex;
            }
            if (valueLabel != null)
            {
                valueLabel.text = $"Value: {consumable.value}";
                valueLabel.style.display = DisplayStyle.Flex;
            }
            if (durationLabel != null)
            {
                durationLabel.text = $"Duration: {consumable.duration}";
                durationLabel.style.display = DisplayStyle.Flex;
            }
            if (cooldownLabel != null)
            {
                cooldownLabel.text = $"Cooldown: {consumable.cooldown}";
                cooldownLabel.style.display = DisplayStyle.Flex;
            }
            if (maxStackLabel != null)
            {
                maxStackLabel.text = $"Max Stack: {consumable.maxStack}";
                maxStackLabel.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// Show weapon-specific stats using the same label set.
        /// </summary>
        public void ShowWeaponStats(WeaponItemData weapon)
        {
            if (effectLabel != null)
            {
                effectLabel.text = $"Type: {weapon.weaponType}";
                effectLabel.style.display = DisplayStyle.Flex;
            }
            if (valueLabel != null)
            {
                valueLabel.text = $"Damage: {weapon.damage}";
                valueLabel.style.display = DisplayStyle.Flex;
            }
            if (durationLabel != null)
            {
                durationLabel.text = $"Range: {weapon.range}";
                durationLabel.style.display = DisplayStyle.Flex;
            }
            if (cooldownLabel != null)
            {
                cooldownLabel.text = $"Attack Speed: {weapon.attackSpeed}";
                cooldownLabel.style.display = DisplayStyle.Flex;
            }
            if (maxStackLabel != null)
            {
                maxStackLabel.text = $"Ammo: {weapon.ammo}";
                maxStackLabel.style.display = DisplayStyle.Flex;
            }
        }

        public void HideAllStats()
        {
            if (effectLabel != null)
                effectLabel.style.display = DisplayStyle.None;
            if (valueLabel != null)
                valueLabel.style.display = DisplayStyle.None;
            if (durationLabel != null)
                durationLabel.style.display = DisplayStyle.None;
            if (cooldownLabel != null)
                cooldownLabel.style.display = DisplayStyle.None;
            if (maxStackLabel != null)
                maxStackLabel.style.display = DisplayStyle.None;
        }
    }
}
