using System;
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

    public void SetCurrentWorld(Models.WorldMetadata world) {
        logger.Log($"Setting current world info: {world.name}", this);

        Label worldNameLabel = ui.Q<Label>("Title");
        if (worldNameLabel != null) {
            worldNameLabel.text = world.name.Split('.')[0];
        } else {
            logger.Log("WorldNameLabel not found in UI", this, Logging.LogType.Warning);
        }

        Label worldCreatedAtLabel = ui.Q<Label>("CreatedAt");
        if (worldCreatedAtLabel != null) {
            worldCreatedAtLabel.text = $"Created {makeHumanReadableCreatedAt(world.createdAt)}";
        } else {
            logger.Log("WorldCreatedAtLabel not found in UI", this, Logging.LogType.Warning);
        }

        Label worldCreatorLabel = ui.Q<Label>("CreatedBy");
        if (worldCreatorLabel != null) {
            string displayName = getUserDisplayName(world.userId);
            worldCreatorLabel.text = $"Created By {displayName}";
        } else {
            logger.Log("WorldCreatorLabel not found in UI", this, Logging.LogType.Warning); 
        }
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

        Button closeButton = ui.Q<Button>("CloseButton");
        closeButton.clicked += OnCloseButtonClicked;
    }

    private void OnCloseButtonClicked() {
        logger.Log("Close button clicked", this);
        gameObject.SetActive(false);
    }
}
