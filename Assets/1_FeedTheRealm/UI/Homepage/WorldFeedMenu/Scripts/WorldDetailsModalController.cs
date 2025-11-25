using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// Controls the World Details modal popup, displaying information about a selected world.
/// </summary>
public class WorldDetailsModalController : MonoBehaviour {
    [SerializeField]
    private Logging.Logger logger;

    private VisualElement modalOverlay;
    private Label worldNameLabel;
    private Label creatorLabel;
    private Label playersOnlineLabel;
    private Label createdAtLabel;
    private Label updatedAtLabel;
    private Label descriptionLabel;
    private Button cancelButton;
    private Button closeButton;

    private API.WorldsData currentWorldData;
    private Action<API.WorldsData> onJoinWorldCallback;

    /// <summary>
    /// Initializes the modal controller with UI references.
    /// </summary>
    /// <param name="rootElement">The root visual element containing the modal</param>
    public void Initialize(VisualElement rootElement) {
        if (rootElement == null) {
            logger.Log("Root element is null, cannot initialize modal", this, Logging.LogType.Error);
            return;
        }

        // Get modal elements
        modalOverlay = rootElement.Q<VisualElement>("ModalOverlay");
        worldNameLabel = rootElement.Q<Label>("WorldNameLabel");
        creatorLabel = rootElement.Q<Label>("CreatorLabel");
        playersOnlineLabel = rootElement.Q<Label>("PlayersOnlineLabel");
        createdAtLabel = rootElement.Q<Label>("CreatedAtLabel");
        updatedAtLabel = rootElement.Q<Label>("UpdatedAtLabel");
        descriptionLabel = rootElement.Q<Label>("DescriptionLabel");
        cancelButton = rootElement.Q<Button>("CancelButton");
        closeButton = rootElement.Q<Button>("CloseButton");

        // Register button callbacks
        if (cancelButton != null) {
            cancelButton.clicked += Hide;
        }

        if (closeButton != null) {
            closeButton.clicked += Hide;
        }

        // Initially hide the modal
        Hide();

        logger.Log("World Details Modal initialized successfully", this);
    }

    /// <summary>
    /// Shows the modal with world details.
    /// </summary>
    public void Show(API.WorldsData worldData) {
        if (worldData == null) {
            logger.Log("Cannot show modal: world data is null", this, Logging.LogType.Error);
            return;
        }

        currentWorldData = worldData;
        PopulateModalData(worldData);

        if (modalOverlay != null) {
            modalOverlay.style.display = DisplayStyle.Flex;
            logger.Log($"Showing world details for: {worldData.name}", this);
        }
    }

    /// <summary>
    /// Hides the modal.
    /// </summary>
    public void Hide() {
        if (modalOverlay != null) {
            modalOverlay.style.display = DisplayStyle.None;
            logger.Log("World details modal hidden", this);
        }
    }

    /// <summary>
    /// Populates the modal with world data.
    /// </summary>
    private void PopulateModalData(API.WorldsData worldData) {
        if (worldNameLabel != null) {
            worldNameLabel.text = worldData.name ?? "Unknown World";
        }

        if (creatorLabel != null) {
            // Extract username from user_id if possible, or use the full ID
            creatorLabel.text = worldData.user_id ?? "Unknown";
        }
    }

    /// <summary>
    /// Formats a date string for display.
    /// </summary>
    private string FormatDate(string dateString) {
        if (string.IsNullOrEmpty(dateString)) {
            return "-";
        }

        try {
            DateTime date = DateTime.Parse(dateString);
            return date.ToString("MMM dd, yyyy HH:mm");
        } catch {
            return dateString; // Return as-is if parsing fails
        }
    }

    private void OnDestroy() {
        // Cleanup event handlers
        if (cancelButton != null) {
            cancelButton.clicked -= Hide;
        }

        if (closeButton != null) {
            closeButton.clicked -= Hide;
        }
    }
}
