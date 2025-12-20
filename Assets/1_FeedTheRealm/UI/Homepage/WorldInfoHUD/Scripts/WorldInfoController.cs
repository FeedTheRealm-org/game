using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class WorldInfoController : MonoBehaviour {
    [SerializeField]
    private Logging.Logger logger;

    private VisualElement ui;

    public void SetCurrentWorld(Models.WorldMetadata world) {
        logger.Log($"Setting current world info: {world.name}", this);

        Label worldNameLabel = ui.Q<Label>("Title");
        if (worldNameLabel != null) {
            worldNameLabel.text = world.name;
        } else {
            logger.Log("WorldNameLabel not found in UI", this, Logging.LogType.Warning);
        }

        Label worldCreatedAtLabel = ui.Q<Label>("CreatedAt");
        if (worldCreatedAtLabel != null) {
            worldCreatedAtLabel.text = $"Created At: {world.createdAt}";
        } else {
            logger.Log("WorldCreatedAtLabel not found in UI", this, Logging.LogType.Warning);
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
