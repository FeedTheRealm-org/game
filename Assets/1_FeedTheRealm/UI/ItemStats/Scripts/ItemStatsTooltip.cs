using FTR.Gameplay.Client.Registry;
using FTRShared.Runtime.Models;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Tooltip controller for displaying item statistics on hover.
/// Shows stats for items using data provided by the current world.
///
/// For gameplay items, data comes from the current world's item collections
/// (via ClientItemsRegistry). Items are identified by their unique item id.
/// </summary>
public class ItemStatsTooltip : MonoBehaviour
{
    [Header("Logging")]
    [SerializeField]
    private Logging.Logger logger;

    [Header("Description Wrapping")]
    [Tooltip("Maximum characters per line before inserting a newline for the Description label.")]
    [SerializeField]
    private int descriptionMaxLineLength = 25;

    private VisualElement tooltipContainer;
    private Label nameLabel;
    private Label descriptionLabel;
    private VisualElement divider;
    private StatsPresenter statsPresenter;

    private bool isInitialized;
    private bool isVisible;
    private string currentItemId;

    public void Initialize(VisualElement container)
    {
        if (container == null)
        {
            return;
        }

        tooltipContainer = container;
        InitializeUIElements(container);
    }

    private void InitializeUIElements(VisualElement root)
    {
        nameLabel = root.Q<Label>("Name");
        descriptionLabel = root.Q<Label>("Description");
        divider = root.Q<VisualElement>("Divider");

        statsPresenter = new StatsPresenter(
            root.Q<Label>("Effect"),
            root.Q<Label>("Value"),
            root.Q<Label>("Duration"),
            root.Q<Label>("Cooldown"),
            root.Q<Label>("MaxStack")
        );

        isInitialized = true;
        HideTooltip();
    }

    public void ShowTooltip(string itemId, VisualElement slot)
    {
        if (
            !isInitialized
            || string.IsNullOrEmpty(itemId)
            || slot == null
            || tooltipContainer == null
        )
        {
            Debug.LogWarning(
                $"[Tooltip] Guard failed — isInitialized:{isInitialized} | itemIdEmpty:{string.IsNullOrEmpty(itemId)} | slotNull:{slot == null} | containerNull:{tooltipContainer == null}"
            );
            return;
        }

        var item = ClientItemsRegistry.GetItemById(itemId);
        if (item == null)
        {
            logger?.Log(
                $"[Tooltip] Item not found for id: {itemId}",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        currentItemId = itemId;

        if (nameLabel != null)
        {
            nameLabel.text = item.name;
            nameLabel.style.display = DisplayStyle.Flex;
        }

        if (descriptionLabel != null)
        {
            string desc = item.description ?? "";
            if (desc.Length > 100)
                desc = desc.Substring(0, 97) + "...";
            descriptionLabel.text = TooltipTextUtils.InsertLineBreaks(
                desc,
                descriptionMaxLineLength
            );
            descriptionLabel.style.display = DisplayStyle.Flex;
        }

        statsPresenter?.ShowStats(item);

        if (divider != null)
        {
            bool hasStats = item is ConsumableItemData || item is WeaponItemData;
            divider.style.display = hasStats ? DisplayStyle.Flex : DisplayStyle.None;
        }

        UpdateTooltipPosition(slot);

        tooltipContainer.style.display = DisplayStyle.Flex;
        isVisible = true;
    }

    public void HideTooltip()
    {
        if (tooltipContainer != null)
            tooltipContainer.style.display = DisplayStyle.None;

        isVisible = false;
        currentItemId = null;
    }

    public bool IsVisible() => isVisible;

    public string GetCurrentItemId() => currentItemId;

    private void UpdateTooltipPosition(VisualElement slot)
    {
        if (tooltipContainer == null || slot == null)
            return;
        if (tooltipContainer.panel == null)
            return;

        var panelRoot = tooltipContainer.panel.visualTree;
        float panelW = panelRoot.resolvedStyle.width;
        float panelH = panelRoot.resolvedStyle.height;

        var slotBounds = slot.worldBound;
        float tooltipW = 420f;
        float tooltipH = 260f;
        float offset = 12f;
        float margin = 12f;

        float left;
        bool slotIsOnRightHalf = slotBounds.xMin > panelW * 0.5f;
        if (slotIsOnRightHalf)
        {
            left = slotBounds.xMin - tooltipW - offset;
            if (left < margin)
                left = margin;
        }
        else
        {
            left = slotBounds.xMax + offset;
            if (left + tooltipW > panelW - margin)
                left = panelW - tooltipW - margin;
        }

        float top = slotBounds.yMin;
        if (top + tooltipH > panelH - margin)
            top = panelH - tooltipH - margin;
        top = Mathf.Max(margin, top);

        tooltipContainer.style.left = left;
        tooltipContainer.style.top = top;

        tooltipContainer
            .schedule.Execute(() =>
            {
                if (!isVisible)
                    return;
                float realH = tooltipContainer.resolvedStyle.height;
                if (realH <= 0 || float.IsNaN(realH))
                    return;

                float adjustedTop = slotBounds.yMin;
                if (adjustedTop + realH > panelH - margin)
                    adjustedTop = panelH - realH - margin;
                tooltipContainer.style.top = Mathf.Max(margin, adjustedTop);
            })
            .StartingIn(1);
    }

    private class StatsPresenter
    {
        private readonly Label effectLabel;
        private readonly Label valueLabel;
        private readonly Label durationLabel;
        private readonly Label cooldownLabel;
        private readonly Label maxStackLabel;

        public StatsPresenter(
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

        public void ShowStats(ItemData item)
        {
            HideAll();
            switch (item)
            {
                case ConsumableItemData consumable:
                    ShowConsumable(consumable);
                    break;
                case WeaponItemData weapon:
                    ShowWeapon(weapon);
                    break;
            }
        }

        private void ShowConsumable(ConsumableItemData c)
        {
            SetLabel(effectLabel, $"Effect: {c.effectType}");
            SetLabel(valueLabel, $"Value: {c.value}");
            SetLabel(durationLabel, $"Duration: {c.duration}");
            SetLabel(cooldownLabel, $"Cooldown: {c.cooldown}");
            SetLabel(maxStackLabel, $"Max Stack: {c.maxStack}");
        }

        private void ShowWeapon(WeaponItemData w)
        {
            SetLabel(effectLabel, $"Type: {w.weaponType}");
            SetLabel(valueLabel, $"Damage: {w.damage}");
            SetLabel(durationLabel, $"Range: {w.range}");
            SetLabel(cooldownLabel, $"Attack Speed: {w.attackSpeed}");
            SetLabel(maxStackLabel, $"Ammo: {w.ammo}");
        }

        private static void SetLabel(Label label, string text)
        {
            if (label == null)
                return;
            label.text = text;
            label.style.display = DisplayStyle.Flex;
        }

        private void HideAll()
        {
            Hide(effectLabel);
            Hide(valueLabel);
            Hide(durationLabel);
            Hide(cooldownLabel);
            Hide(maxStackLabel);
        }

        private static void Hide(Label label)
        {
            if (label != null)
                label.style.display = DisplayStyle.None;
        }
    }
}
