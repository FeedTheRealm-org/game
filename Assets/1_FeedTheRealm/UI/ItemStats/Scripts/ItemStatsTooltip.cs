using UnityEngine;
using UnityEngine.UIElements;
using API;
using Items;

/// <summary>
/// Tooltip controller for displaying item statistics on hover.
/// Shows dynamic stats based on item category (weapon/armor).
/// Uses mock data until backend provides real stats.
/// </summary>
public class ItemStatsTooltip : MonoBehaviour {
    [Header("UI References")]
    [SerializeField] private UIDocument tooltipDocument;

    [Header("Mock Stats Configuration")]
    [SerializeField] private int weaponAttackValue = 10;
    [SerializeField] private float weaponAttackSpeedValue = 1.5f;
    [SerializeField] private int weaponAttackRangeValue = 5;

    [SerializeField] private int armorDefenseValue = 8;
    [SerializeField] private int armorResistanceValue = 12;

    [Header("Logging")]
    [SerializeField] private Logging.Logger logger;

    // UI Elements
    private VisualElement root;
    private VisualElement tooltipContainer;
    private Label nameLabel;
    private Label descriptionLabel;
    private Label attackLabel;
    private Label defenseLabel;
    private Label attackSpeedLabel;
    private Label resistanceLabel;
    private Label attackRangeLabel;

    // State
    private bool isVisible = false;
    private string currentItemId;

    void Awake() {
        if (tooltipDocument == null) {
            tooltipDocument = GetComponent<UIDocument>();
        }

        if (tooltipDocument != null) {
            root = tooltipDocument.rootVisualElement;
            InitializeUIElements();
            HideTooltip();
        } else {
            logger?.Log("UIDocument not assigned!", this, Logging.LogType.Error);
        }
    }

    /// <summary>
    /// Initialize references to all UI elements.
    /// </summary>
    private void InitializeUIElements() {
        // Get the main container by name
        tooltipContainer = root.Q<VisualElement>("TooltipContainer");

        if (tooltipContainer == null) {
            logger?.Log("TooltipContainer not found! Make sure UXML has a VisualElement named 'TooltipContainer'", this, Logging.LogType.Error);
            return;
        }

        // Get all labels
        nameLabel = root.Q<Label>("Name");
        descriptionLabel = root.Q<Label>("Description");
        attackLabel = root.Q<Label>("Attack");
        defenseLabel = root.Q<Label>("Defense");
        attackSpeedLabel = root.Q<Label>("AttackSpeed");
        resistanceLabel = root.Q<Label>("Resistance");
        attackRangeLabel = root.Q<Label>("AttackRange");

        // Validate that all elements were found
        if (nameLabel == null || descriptionLabel == null) {
            logger?.Log("Failed to find required UI labels!", this, Logging.LogType.Error);
        }

        logger?.Log("ItemStatsTooltip UI elements initialized", this);
    }

    /// <summary>
    /// Show tooltip for a specific item next to the slot.
    /// </summary>
    public void ShowTooltip(string itemId, VisualElement slot) {
        if (string.IsNullOrEmpty(itemId)) {
            logger?.Log("Cannot show tooltip: itemId is null or empty", this, Logging.LogType.Warning);
            return;
        }

        if (slot == null) {
            logger?.Log("Cannot show tooltip: slot is null", this, Logging.LogType.Warning);
            return;
        }

        // Get item metadata from ItemsManager
        var itemsManager = ItemsManager.Instance;
        if (itemsManager == null || !itemsManager.IsInitialized) {
            logger?.Log("ItemsManager not available or not initialized", this, Logging.LogType.Warning);
            return;
        }

        var itemData = itemsManager.GetItemById(itemId);
        if (itemData == null) {
            logger?.Log($"Item not found: {itemId}", this, Logging.LogType.Warning);
            return;
        }

        currentItemId = itemId;
        PopulateTooltipData(itemData);
        UpdateTooltipPosition(slot);

        tooltipContainer.style.display = DisplayStyle.Flex;
        isVisible = true;

        var categoryName = ItemsManager.Instance?.GetCategoryNameById(itemData.category_id) ?? string.Empty;
        logger?.Log($"Tooltip shown for item: {itemData.name} ({categoryName})", this);
    }

    /// <summary>
    /// Hide the tooltip.
    /// </summary>
    public void HideTooltip() {
        if (tooltipContainer != null) {
            tooltipContainer.style.display = DisplayStyle.None;
        }
        isVisible = false;
        currentItemId = null;

        logger?.Log("Tooltip hidden", this);
    }

    /// <summary>
    /// Populate tooltip with item data and mock stats.
    /// Shows/hides labels based on item category.
    /// </summary>
    private void PopulateTooltipData(ItemMetadataResponse itemData) {
        // Always show Name and Description
        if (nameLabel != null) {
            nameLabel.text = itemData.name;
            nameLabel.style.display = DisplayStyle.Flex;
        }

        if (descriptionLabel != null) {
            descriptionLabel.text = itemData.description;
            descriptionLabel.style.display = DisplayStyle.Flex;
        }

        // Show/hide stats based on category
        var categoryName = ItemsManager.Instance?.GetCategoryNameById(itemData.category_id) ?? string.Empty;
        switch (categoryName.ToLower()) {
            case "weapon":
                ShowWeaponStats();
                HideArmorStats();
                break;

            case "armor":
                ShowArmorStats();
                HideWeaponStats();
                break;

            default:
                // Unknown category - hide all stats
                HideWeaponStats();
                HideArmorStats();
                logger?.Log($"Unknown category: {categoryName}", this, Logging.LogType.Warning);
                break;
        }
    }

    /// <summary>
    /// Show weapon-specific stats with mock values.
    /// </summary>
    private void ShowWeaponStats() {
        if (attackLabel != null) {
            attackLabel.text = $"Attack: {weaponAttackValue}";
            attackLabel.style.display = DisplayStyle.Flex;
        }

        if (attackSpeedLabel != null) {
            attackSpeedLabel.text = $"Attack Speed: {weaponAttackSpeedValue}";
            attackSpeedLabel.style.display = DisplayStyle.Flex;
        }

        if (attackRangeLabel != null) {
            attackRangeLabel.text = $"Attack Range: {weaponAttackRangeValue}";
            attackRangeLabel.style.display = DisplayStyle.Flex;
        }
    }

    /// <summary>
    /// Hide weapon-specific stats.
    /// </summary>
    private void HideWeaponStats() {
        if (attackLabel != null) {
            attackLabel.style.display = DisplayStyle.None;
        }

        if (attackSpeedLabel != null) {
            attackSpeedLabel.style.display = DisplayStyle.None;
        }

        if (attackRangeLabel != null) {
            attackRangeLabel.style.display = DisplayStyle.None;
        }
    }

    /// <summary>
    /// Show armor-specific stats with mock values.
    /// </summary>
    private void ShowArmorStats() {
        if (defenseLabel != null) {
            defenseLabel.text = $"Defense: {armorDefenseValue}";
            defenseLabel.style.display = DisplayStyle.Flex;
        }

        if (resistanceLabel != null) {
            resistanceLabel.text = $"Resistance: {armorResistanceValue}";
            resistanceLabel.style.display = DisplayStyle.Flex;
        }
    }

    /// <summary>
    /// Hide armor-specific stats.
    /// </summary>
    private void HideArmorStats() {
        if (defenseLabel != null) {
            defenseLabel.style.display = DisplayStyle.None;
        }

        if (resistanceLabel != null) {
            resistanceLabel.style.display = DisplayStyle.None;
        }
    }

    /// <summary>
    /// Update tooltip position to appear next to the slot.
    /// Positions tooltip to the right of the slot, or to the left if not enough space.
    /// </summary>
    private void UpdateTooltipPosition(VisualElement slot) {
        if (tooltipContainer == null) {
            logger?.Log("[Tooltip] UpdateTooltipPosition: tooltipContainer is null", this, Logging.LogType.Warning);
            return;
        }

        if (slot == null) {
            logger?.Log("[Tooltip] UpdateTooltipPosition: slot is null", this, Logging.LogType.Warning);
            return;
        }

        if (tooltipContainer.panel == null) {
            logger?.Log("[Tooltip] UpdateTooltipPosition: panel is null", this, Logging.LogType.Warning);
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
        float tooltipWidth = 200; // Approximate tooltip width, adjust if needed
        float horizontalOffset = 135; // Space between slot and tooltip

        // Position tooltip to the right of the slot by default
        float tooltipLeft = slotPanelPos.x + slotBounds.width + horizontalOffset;

        // If tooltip doesn't fit on the right, position it on the left
        if (tooltipLeft + tooltipWidth > panelWidth) {
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
    public bool IsVisible() {
        return isVisible;
    }

    /// <summary>
    /// Get the current item ID being displayed.
    /// </summary>
    public string GetCurrentItemId() {
        return currentItemId;
    }
}
