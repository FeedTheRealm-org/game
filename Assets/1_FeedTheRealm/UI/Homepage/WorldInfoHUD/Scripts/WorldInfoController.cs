using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class WorldInfoController : MonoBehaviour {
    [SerializeField]
    private Logging.Logger logger;
    [SerializeField]
    private API.PlayerService playerService;

    private VisualElement ui;
    private Button CloseButton;
    private Label WorldNameLabel;
    private Label WorldDescriptionLabel;
    private Label WorldCreatedAtLabel;
    private Label WorldCreatorLabel;

    public void SetCurrentWorld(Models.WorldMetadata world) {
        logger.Log($"Setting current world info: {world.name}", this);

        WorldNameLabel.text = world.name.Split('.')[0];
        WorldDescriptionLabel.text = world.description;
        WorldCreatedAtLabel.text = $"Created {makeHumanReadableCreatedAt(world.createdAt)}";

        string displayName = getUserDisplayName(world.userId);
        WorldCreatorLabel.text = $"Created By {displayName}";
    }

    private string getUserDisplayName(string userId) {
        string displayName = "Unknown User";

        playerService.GetCharacterInfo((characterInfo, error) => {
            if (!string.IsNullOrEmpty(error)) {
                logger.Log($"Error fetching character info for userId {userId}: {error}", this, Logging.LogType.Error);
                return;
            }

            if (characterInfo != null && !string.IsNullOrEmpty(characterInfo.character_name)) {
                displayName = characterInfo.character_name;
            } else {
                logger.Log($"Character info is null or displayName is empty for userId {userId}", this, Logging.LogType.Warning);
            }
        }, userId);

        return displayName;
    }

    private string makeHumanReadableCreatedAt(string createdAt) {
        if (DateTime.TryParse(createdAt, out DateTime parsedDate)) {
            TimeSpan span = DateTime.UtcNow - parsedDate.ToUniversalTime();

            if (span.TotalSeconds < 60) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} minute{((int)span.TotalMinutes == 1 ? "" : "s")} ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hour{((int)span.TotalHours == 1 ? "" : "s")} ago";
            if (span.TotalDays < 30) return $"{(int)span.TotalDays} day{((int)span.TotalDays == 1 ? "" : "s")} ago";

            if (span.TotalDays < 365) {
                int months = (int)(span.TotalDays / 30);
                return $"{months} month{(months == 1 ? "" : "s")} ago";
            }

            int years = (int)(span.TotalDays / 365);
            return $"{years} year{(years == 1 ? "" : "s")} ago";
        } else {
            logger.Log($"Failed to parse createdAt date: {createdAt}", this, Logging.LogType.Warning);
            return createdAt;
        }
    }

    private void OnEnable() {
        ui = GetComponent<UIDocument>().rootVisualElement;

        CloseButton = ui.Q<Button>("CloseButton");
        if (CloseButton == null) {
            logger.Log("CloseButton not found in UI", this, Logging.LogType.Warning);
            return;
        }
        CloseButton.clicked += OnCloseButtonClicked;

        WorldNameLabel = ui.Q<Label>("Title");
        if (WorldNameLabel == null) {
            logger.Log("WorldNameLabel not found in UI", this, Logging.LogType.Warning);
        }

        WorldDescriptionLabel = ui.Q<Label>("Description");
        if (WorldDescriptionLabel == null) {
            logger.Log("WorldDescriptionLabel not found in UI", this, Logging.LogType.Warning);
        }

        WorldCreatedAtLabel = ui.Q<Label>("CreatedAt");
        if (WorldCreatedAtLabel == null) {
            logger.Log("WorldCreatedAtLabel not found in UI", this, Logging.LogType.Warning);
        }

        WorldCreatorLabel = ui.Q<Label>("CreatedBy");
        if (WorldCreatorLabel == null) {
            logger.Log("WorldCreatorLabel not found in UI", this, Logging.LogType.Warning);
        }
    }

    private void OnCloseButtonClicked() {
        logger.Log("Close button clicked", this);
        gameObject.SetActive(false);
    }
}
