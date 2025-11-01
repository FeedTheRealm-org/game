using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages the character editing interface and interactions.
/// </summary>
public class CharacterEditController : MonoBehaviour {
    [SerializeField]
    private Logging.Logger logger;

    [Header("Session settings")]
    [SerializeField]
    private Session.Session session;

    // Containers
    private VisualElement _characterContainer;
    private VisualElement _characterPreview;
    private VisualElement _cosmeticsContainer;

    private Label _errorMessage;

    // Inputs
    private TextField _nameInput;
    private TextField _bioInput;

    // Buttons
    private Button _backButton;
    private Button _cancelButton;
    private Button _saveButton;

    private void OnEnable() {
        if (session == null) {
            logger.Log("Session is not assigned.", this, Logging.LogType.Error);
            return;
        }

        var root = GetComponent<UIDocument>().rootVisualElement;
        var body = root.Q<VisualElement>("Body");
        if (body == null) {
            logger.Log("Body not found in the UI Document.", this, Logging.LogType.Error);
            return;
        }

        // Containers
        var container = body.Q<VisualElement>("CharacterEditContainer");
        if (container == null) {
            logger.Log("CharacterEditContainer not found in the UI Document.", this, Logging.LogType.Error);
            return;
        }
        _characterContainer = container.Q<VisualElement>("Character");
        _cosmeticsContainer = container.Q<VisualElement>("Cosmetics");
        if (_characterContainer == null || _cosmeticsContainer == null) {
            logger.Log("Character or Cosmetics not found in the UI Document.", this, Logging.LogType.Error);
            return;
        }

        _characterPreview = _characterContainer.Q<VisualElement>("CharacterPreview");
        if (_characterPreview == null) {
            logger.Log("CharacterPreview not found in Character container.", this, Logging.LogType.Error);
            return;
        }

        var buttonsContainer = _cosmeticsContainer.Q<VisualElement>("Buttons");
        if (buttonsContainer == null) {
            logger.Log("Buttons container not found in Cosmetics.", this, Logging.LogType.Error);
            return;
        }

        // Buttons and Inputs
        _nameInput = _characterContainer.Q<TextField>("NameInput");
        _bioInput = _characterContainer.Q<TextField>("BioInput");

        _cancelButton = buttonsContainer.Q<Button>("Cancel");
        _saveButton = buttonsContainer.Q<Button>("Save");
        _backButton = body.Q<Button>("BackButton");

        _errorMessage = _cosmeticsContainer.Q<Label>("ErrorMessage");

        if (_nameInput == null || _bioInput == null || _backButton == null || _cancelButton == null || _saveButton == null || _errorMessage == null) {
            logger.Log("Buttons or Inputs not found in UI Document.", this, Logging.LogType.Error);
            return;
        }

        if (session.IsFirstLogin) {
            logger.Log("First login detected, hiding back button.", this);
            _backButton.style.display = DisplayStyle.None;
        }

        registerCallbacks();
    }

    /// <summary>
    /// Registers button click callbacks.
    /// </summary>
    private void registerCallbacks() {
        _backButton.clicked += onBackClicked;
        _cancelButton.clicked += onCancelClicked;
        _saveButton.clicked += onSaveClicked;
    }

    private void onBackClicked() {
        logger.Log("Back Button Clicked", this);
    }

    private void onCancelClicked() {
        logger.Log("Cancel Button Clicked", this);
    }

    private void onSaveClicked() {
        logger.Log("Save Button Clicked", this);
        logger.Log($"Name: {_nameInput.value}, Bio {_bioInput.value}", this);
        session.IsFirstLogin = false;
    }
}
