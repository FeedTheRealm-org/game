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

    [Header("Tooltip Settings")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(15, -15);

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
    /// Show tooltip for a specific item at cursor position.
    /// </summary>
    public void ShowTooltip(string itemId, Vector2 cursorPosition) {
        if (string.IsNullOrEmpty(itemId)) {
            logger?.Log("Cannot show tooltip: itemId is null or empty", this, Logging.LogType.Warning);
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
        UpdateTooltipPosition(cursorPosition);

        tooltipContainer.style.display = DisplayStyle.Flex;
        isVisible = true;

        logger?.Log($"Tooltip shown for item: {itemData.name} ({itemData.category})", this);
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
        switch (itemData.category.ToLower()) {
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
                logger?.Log($"Unknown category: {itemData.category}", this, Logging.LogType.Warning);
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
    /// Update tooltip position to follow cursor with offset.
    /// </summary>
    private void UpdateTooltipPosition(Vector2 cursorPosition) {
        if (tooltipContainer == null) {
            logger?.Log("[Tooltip] UpdateTooltipPosition: tooltipContainer is null", this, Logging.LogType.Warning);
            return;
        }

        if (tooltipContainer.panel == null) {
            logger?.Log("[Tooltip] UpdateTooltipPosition: panel is null", this, Logging.LogType.Warning);
            return;
        }

        // Convert screen coordinates to UI Toolkit coordinates
        // UI Toolkit uses top-left origin, cursor uses bottom-left
        Vector2 uiPosition = RuntimePanelUtils.ScreenToPanel(
            tooltipContainer.panel,
            new Vector2(cursorPosition.x, Screen.height - cursorPosition.y)
        );

        // Apply offset
        tooltipContainer.style.left = uiPosition.x + cursorOffset.x;
        tooltipContainer.style.top = uiPosition.y + cursorOffset.y;
        tooltipContainer.style.position = Position.Absolute;

        logger?.Log($"[Tooltip] Position updated: ({uiPosition.x + cursorOffset.x}, {uiPosition.y + cursorOffset.y})", this);
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
