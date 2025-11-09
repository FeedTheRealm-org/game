using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// Manages the character editing interface and interactions.
/// </summary>
public class CharacterEditController : MonoBehaviour {
    [Header("Session settings")]
    [SerializeField]
    private API.PlayerService playerService;

    [SerializeField]
    private Session.Session session;

    [SerializeField]
    private SpriteManager spriteManager;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    // Containers
    private VisualElement _characterContainer;
    private VisualElement _characterPreview;
    private VisualElement _cosmeticsContainer;
    private ScrollView _itemsList;
    private ScrollView _categoriesList;

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
        _itemsList = _cosmeticsContainer.Q<ScrollView>("Items");
        _categoriesList = _cosmeticsContainer.Q<ScrollView>("Categories");

        if (_nameInput == null || _bioInput == null || _backButton == null || _cancelButton == null || _saveButton == null || _errorMessage == null) {
            logger.Log("Buttons or Inputs not found in UI Document.", this, Logging.LogType.Error);
            return;
        }

        if (session.IsFirstLogin) {
            logger.Log("First login detected, hiding back button.", this);
            _backButton.style.display = DisplayStyle.None;
        }

        registerCallbacks(true);
        fetchCharacterInfo();
    }

    private void OnDisable() {
        registerCallbacks(false);
    }

    /// <summary>
    /// Registers button click callbacks.
    /// </summary>
    private void registerCallbacks(bool shouldRegister) {
        if (shouldRegister) {
            logger.Log("Registering button callbacks", this);
            _backButton.clicked += onBackClicked;
            _cancelButton.clicked += onCancelClicked;
            _saveButton.clicked += onSaveClicked;
        } else {
            logger.Log("Unregistering button callbacks", this);
            _backButton.clicked -= onBackClicked;
            _cancelButton.clicked -= onCancelClicked;
            _saveButton.clicked -= onSaveClicked;
        }
        registerListButtonsCallbacks(_itemsList, shouldRegister, onItemClicked);
        registerListButtonsCallbacks(_categoriesList, shouldRegister, onCategoryClicked);
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

        _errorMessage.text = "";
        if (_nameInput.value == "") {
            _errorMessage.text = "Name cannot be empty.";
            return;
        }

        updateCharacterInfo();
    }

    /// <summary>
    /// Registers callbacks for item buttons in the items list.
    /// </summary>
    private void registerListButtonsCallbacks(ScrollView container, bool shouldRegister, Action<Button> handler) {
        foreach (var child in container.contentContainer.Children()) {
            logger.Log($"Registering callback for item: {child.name}", this);
            if (child is Button btn) {
                if (shouldRegister) {
                    btn.clicked += () => handler(btn);
                } else {
                    btn.clicked -= () => handler(btn);
                }
            }
        }
    }

    /// <summary>
    /// Handles category click events.
    /// </summary>
    private void onCategoryClicked(Button btn) {
        spriteManager.ChangeSprite(SpritePart.Hair, "75624cb9-526c-462f-9d1b-acbf5797b1cb");
        logger.Log($"Category clicked: {btn.name}", this);
    }

    /// <summary>
    /// Handles item click events.
    /// </summary>
    private void onItemClicked(Button btn) {
        spriteManager.ChangeSprite(SpritePart.Hair, "492c228c-ff41-4ad4-a9ed-5225a1a0ea09");
        logger.Log($"Item clicked: {btn.name}", this);
    }

    /// <summary>
    /// Updates the current character information to server.
    /// </summary>
    private void updateCharacterInfo() {
        StartCoroutine(playerService.UpdateCharacterInfo(_nameInput.value, _bioInput.value, (name, bio, err) => {
            if (string.IsNullOrEmpty(err)) {
                logger.Log("Character info successfully updated", this);
                session.IsFirstLogin = false;
                session.CharacterName = name;
            } else {
                logger.Log("Login failed", this, Logging.LogType.Error);
                _errorMessage.text = err;
            }
        }));
    }

    /// <summary>
    /// Fetches the current character information from the server.
    /// </summary>
    private void fetchCharacterInfo() {
        StartCoroutine(playerService.GetCharacterInfo((name, bio, err) => {
            if (string.IsNullOrEmpty(err)) {
                logger.Log("Character info successfully retrieved", this);
                _nameInput.value = name;
                _bioInput.value = bio;
            } else {
                logger.Log("Failed to retrieve character info", this, Logging.LogType.Warning);
            }
        }));
    }
}
