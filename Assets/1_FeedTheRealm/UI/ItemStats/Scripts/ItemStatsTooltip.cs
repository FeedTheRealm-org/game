using FTR.Gameplay.Client.Registry;
using FTRShared.Runtime.Models;
using UnityEngine;
using UnityEngine.Rendering;
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
    [Header("UI References")]
    [SerializeField]
    private UIDocument tooltipDocument;

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

    private string pendingItemId;
    private VisualElement pendingSlot;

    private void Awake()
    {
        if (Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            enabled = false;
            return;
        }

        tooltipDocument ??= GetComponent<UIDocument>();

        if (tooltipDocument == null)
            logger?.Log("UIDocument not assigned!", this, Logging.LogType.Error);
    }

    private void OnEnable()
    {
        if (tooltipDocument == null)
            return;
        TryInitialize();
    }

    private void Update()
    {
        if (!isInitialized)
            TryInitialize();
    }

    private void TryInitialize()
    {
        var root = tooltipDocument.rootVisualElement;

        if (root == null || root.childCount == 0)
            return;

        InitializeUIElements(root);
    }

    private void InitializeUIElements(VisualElement root)
    {
        // TooltipContainer sits directly under the panel root,
        // so index access is more reliable than Q() in this case.
        tooltipContainer =
            root.childCount > 0 ? root[0] : root.Q<VisualElement>("TooltipContainer");

        if (tooltipContainer == null)
        {
            logger?.Log("[Tooltip] TooltipContainer not found!", this, Logging.LogType.Error);
            return;
        }

        nameLabel = tooltipContainer.Q<Label>("Name");
        descriptionLabel = tooltipContainer.Q<Label>("Description");
        divider = tooltipContainer.Q<VisualElement>("Divider");

        statsPresenter = new StatsPresenter(
            tooltipContainer.Q<Label>("Effect"),
            tooltipContainer.Q<Label>("Value"),
            tooltipContainer.Q<Label>("Duration"),
            tooltipContainer.Q<Label>("Cooldown"),
            tooltipContainer.Q<Label>("MaxStack")
        );

        if (nameLabel == null || descriptionLabel == null)
            logger?.Log(
                "[Tooltip] Failed to find required UI labels!",
                this,
                Logging.LogType.Error
            );

        isInitialized = true;
        HideTooltip();
        logger?.Log("[Tooltip] UI elements initialized successfully", this);

        // Show tooltip if the user was already hovering during initialization
        if (!string.IsNullOrEmpty(pendingItemId) && pendingSlot != null)
        {
            ShowTooltip(pendingItemId, pendingSlot);
            pendingItemId = null;
            pendingSlot = null;
        }
    }

    public void ShowTooltip(string itemId, VisualElement slot)
    {
        logger?.Log(
            $"[Tooltip] ShowTooltip called — isInitialized: {isInitialized}, itemId: {itemId}",
            this
        );

        if (!isInitialized)
        {
            pendingItemId = itemId;
            pendingSlot = slot;
            return;
        }

        if (string.IsNullOrEmpty(itemId) || slot == null || tooltipContainer == null)
            return;

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
            {
                desc = desc.Substring(0, 97) + "...";
            }

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
        logger?.Log($"[Tooltip] Shown for itemId: {currentItemId}", this);
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
        if (tooltipContainer?.panel == null || slot == null)
            return;

        var slotBounds = slot.worldBound;
        var slotPanelPos = RuntimePanelUtils.ScreenToPanel(
            tooltipContainer.panel,
            new Vector2(slotBounds.x, slotBounds.y)
        );

        float panelWidth = tooltipContainer.panel.visualTree.worldBound.width;
        float tooltipWidth = 600f;
        float offset = 15f;

        float left = slotPanelPos.x + slotBounds.width + offset;
        if (left + tooltipWidth > panelWidth)
            left = slotPanelPos.x - tooltipWidth - offset;

        tooltipContainer.style.left = left;
        tooltipContainer.style.top = slotPanelPos.y;
        tooltipContainer.style.position = Position.Absolute;
    }

    // -------------------------------------------------------------------------
    // Nested class: no external consumers, no reason to live in its own file
    // -------------------------------------------------------------------------
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
