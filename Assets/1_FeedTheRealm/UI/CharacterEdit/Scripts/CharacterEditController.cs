using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages the character editing interface and interactions.
/// </summary>
public class CharacterEditController : MonoBehaviour {
    [Header("Session settings")]
    [SerializeField]
    private API.PlayerService playerService;

    [SerializeField]
    private API.AssetsService assetsService;

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

    private string _selectedCategoryId = "";

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
        fetchCategories();
        onCategoryClicked(_selectedCategoryId);
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
    /// Handles category click events and fetches sprites for that category.
    /// </summary>
    private void onCategoryClicked(string categoryId) {
        logger.Log($"Category clicked: {categoryId}", this);
        _selectedCategoryId = categoryId;

        StartCoroutine(assetsService.GetSpritesByCategory(categoryId, (response, err) => {
            if (!string.IsNullOrEmpty(err)) {
                logger.Log($"Failed to fetch sprites: {err}", this, Logging.LogType.Error);
                _errorMessage.text = "Failed to load sprites.";
                return;
            }

            if (response == null || response.sprites_list == null) {
                logger.Log("No sprites found for this category.", this, Logging.LogType.Warning);
                _itemsList.contentContainer.Clear();
                return;
            }

            populateItems(response.sprites_list);
        }));
    }

    /// <summary>
    /// Handles item click events and changes the sprite.
    /// </summary>
    private void onItemClicked(string spriteId) {
        logger.Log($"Item clicked: {spriteId}", this);
        spriteManager.ChangeSprite(SpritePart.Hair, spriteId);
    }

    /* --- CHARACTER INFO HANDLING --- */

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

    /* --- CATEGORIES & ITEMS HANDLING --- */

    /// <summary>
    /// Fetches categories from the server and populates the categories list.
    /// </summary>
    private void fetchCategories() {
        StartCoroutine(assetsService.GetCategories((response, err) => {
            if (!string.IsNullOrEmpty(err)) {
                logger.Log($"Failed to fetch categories: {err}", this, Logging.LogType.Error);
                _errorMessage.text = "Failed to load categories.";
                return;
            }

            if (response == null || response.category_list == null) {
                logger.Log("No categories found.", this, Logging.LogType.Warning);
                return;
            }

            populateCategories(response.category_list);
        }));
    }

    /// <summary>
    /// Populates the categories list with buttons.
    /// </summary>
    private void populateCategories(API.SpriteCategoryResponse[] categories) {
        _categoriesList.contentContainer.Clear();

        foreach (var category in categories) {
            var btn = new Button();
            btn.AddToClassList("category_button");
            btn.text = category.category_name;
            btn.name = category.category_id;
            btn.clicked += () => onCategoryClicked(category.category_id);
            _categoriesList.contentContainer.Add(btn);
        }
        if (string.IsNullOrEmpty(_selectedCategoryId)) {
            _selectedCategoryId = categories[0].category_id;
        }
    }

    /// <summary>
    /// Populates the items list with sprite buttons.
    /// </summary>
    private void populateItems(API.SpriteResponse[] sprites) {
        _itemsList.contentContainer.Clear();

        foreach (var sprite in sprites) {
            var btn = new Button();
            btn.AddToClassList("item_button");
            btn.text = sprite.sprite_id;
            btn.name = sprite.sprite_id;
            btn.clicked += () => onItemClicked(sprite.sprite_id);
            _itemsList.contentContainer.Add(btn);
        }
    }
}
