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

        public void ShowConsumableStats(Models.ConsumableItemData consumable)
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
