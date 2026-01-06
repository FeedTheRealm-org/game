using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

/// <summary>
/// Tooltip controller for displaying item statistics on hover.
/// Shows stats for items using data provided by the current world.
///
/// For gameplay items, data comes from WorldData.consumableItems
/// (via Worlds.WorldItemsRegistry). Each item is identified by its
/// spriteId, which is used as the inventory/loot itemId.
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

    // UI Elements
    private VisualElement root;
    private VisualElement tooltipContainer;
    private Label nameLabel;
    private Label descriptionLabel;
    private Label effectLabel;
    private Label valueLabel;
    private Label durationLabel;
    private Label cooldownLabel;
    private Label maxStackLabel;

    // Helpers
    private UI.ItemStats.TooltipStatsPresenter statsPresenter;

    // State
    private bool isVisible = false;
    private string currentItemId;

    void Awake()
    {
        if (Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            enabled = false;
            return;
        }

        if (tooltipDocument == null)
        {
            tooltipDocument = GetComponent<UIDocument>();
        }

        if (tooltipDocument != null)
        {
            root = tooltipDocument.rootVisualElement;
            InitializeUIElements();
            statsPresenter = new UI.ItemStats.TooltipStatsPresenter(
                effectLabel,
                valueLabel,
                durationLabel,
                cooldownLabel,
                maxStackLabel
            );
            HideTooltip();
        }
        else
        {
            logger?.Log("UIDocument not assigned!", this, Logging.LogType.Error);
        }
    }

    /// <summary>
    /// Initialize references to all UI elements.
    /// </summary>
    private void InitializeUIElements()
    {
        // Get the main container by name
        tooltipContainer = root.Q<VisualElement>("TooltipContainer");

        if (tooltipContainer == null)
        {
            logger?.Log(
                "TooltipContainer not found! Make sure UXML has a VisualElement named 'TooltipContainer'",
                this,
                Logging.LogType.Error
            );
            return;
        }

        // Get all labels
        nameLabel = root.Q<Label>("Name");
        descriptionLabel = root.Q<Label>("Description");
        effectLabel = root.Q<Label>("Effect");
        valueLabel = root.Q<Label>("Value");
        durationLabel = root.Q<Label>("Duration");
        cooldownLabel = root.Q<Label>("Cooldown");
        maxStackLabel = root.Q<Label>("MaxStack");
        // Validate that all elements were found
        if (nameLabel == null || descriptionLabel == null)
        {
            logger?.Log("Failed to find required UI labels!", this, Logging.LogType.Error);
        }

        logger?.Log("ItemStatsTooltip UI elements initialized", this);
    }

    /// <summary>
    /// Show tooltip for a specific item next to the slot.
    /// </summary>
    public void ShowTooltip(string itemId, VisualElement slot)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            logger?.Log(
                "Cannot show tooltip: itemId is null or empty",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        if (slot == null)
        {
            logger?.Log("Cannot show tooltip: slot is null", this, Logging.LogType.Warning);
            return;
        }

        // Lookup consumable by item id
        var consumable = Worlds.WorldItemsRegistry.GetConsumableById(itemId);
        if (consumable == null)
        {
            logger?.Log(
                $"Item not found in WorldItemsRegistry for itemId: {itemId}",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        currentItemId = itemId;
        PopulateTooltipDataFromConsumable(consumable);
        UpdateTooltipPosition(slot);

        tooltipContainer.style.display = DisplayStyle.Flex;
        isVisible = true;

        logger?.Log($"Tooltip shown for itemId: {currentItemId}", this);
    }

    /// <summary>
    /// Hide the tooltip.
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipContainer != null)
        {
            tooltipContainer.style.display = DisplayStyle.None;
        }
        isVisible = false;
        currentItemId = null;

        logger?.Log("Tooltip hidden", this);
    }

    /// <summary>
    /// Populate tooltip from world-defined consumable item data.
    /// Uses name/description from the world instead of metadata.
    /// </summary>
    private void PopulateTooltipDataFromConsumable(Models.ConsumableItemData consumable)
    {
        if (consumable == null)
        {
            return;
        }

        if (nameLabel != null)
        {
            nameLabel.text = consumable.name;
            nameLabel.style.display = DisplayStyle.Flex;
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.text = TooltipTextUtils.InsertLineBreaks(
                consumable.description,
                descriptionMaxLineLength
            );
            descriptionLabel.style.display = DisplayStyle.Flex;
        }

        // Show stats using the presenter
        statsPresenter?.HideAllStats();
        statsPresenter?.ShowConsumableStats(consumable);
    }

    /// <summary>
    /// Update tooltip position to appear next to the slot.
    /// Positions tooltip to the right of the slot, or to the left if not enough space.
    /// </summary>
    private void UpdateTooltipPosition(VisualElement slot)
    {
        if (tooltipContainer == null)
        {
            logger?.Log(
                "[Tooltip] UpdateTooltipPosition: tooltipContainer is null",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        if (slot == null)
        {
            logger?.Log(
                "[Tooltip] UpdateTooltipPosition: slot is null",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        if (tooltipContainer.panel == null)
        {
            logger?.Log(
                "[Tooltip] UpdateTooltipPosition: panel is null",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        // Get slot's world bounds
        Rect slotBounds = slot.worldBound;

        // Convert slot position to panel coordinates
        Vector2 slotPanelPos = RuntimePanelUtils.ScreenToPanel(
            tooltipContainer.panel,
            new Vector2(slotBounds.x, slotBounds.y)
        );

        // Calculate panel width to check if tooltip fits on the right
        float panelWidth = tooltipContainer.panel.visualTree.worldBound.width;
        float tooltipWidth = 200;
        float horizontalOffset = 135; // Space between slot and tooltip

        float tooltipLeft = slotPanelPos.x + slotBounds.width + horizontalOffset;

        // If tooltip doesn't fit on the right, position it on the left
        if (tooltipLeft + tooltipWidth > panelWidth)
        {
            tooltipLeft = slotPanelPos.x - tooltipWidth - horizontalOffset;
        }

        // Position vertically aligned with the slot
        float tooltipTop = slotPanelPos.y;

        // Apply position
        tooltipContainer.style.left = tooltipLeft;
        tooltipContainer.style.top = tooltipTop;
        tooltipContainer.style.position = Position.Absolute;

        logger?.Log($"[Tooltip] Position updated: ({tooltipLeft}, {tooltipTop})", this);
    }

    /// <summary>
    /// Check if tooltip is currently visible.
    /// </summary>
    public bool IsVisible()
    {
        return isVisible;
    }

    /// <summary>
    /// Get the current item ID being displayed.
    /// </summary>
    public string GetCurrentItemId()
    {
        return currentItemId;
    }
}
