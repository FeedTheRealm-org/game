using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Manages the character editing interface and interactions.
/// </summary>
public class CharacterEditController : MonoBehaviour
{
    [Header("Session settings")]
    [SerializeField]
    private API.PlayerService playerService;

    [SerializeField]
    private API.AssetsService assetsService;

    [SerializeField]
    private Session.Session session;

    [SerializeField]
    private RectTransform canvasCharacterPreview;

    [SerializeField]
    private Vector2 characterInContainerOffset = new Vector2(-12, 0);

    [SerializeField]
    private SpriteManager spriteManager;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    // Containers
    private VisualElement _root;
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

    // Data
    private string _selectedCategoryId = "";
    private string _selectedCategoryName = "";
    private API.PatchCharacterInfoRequest characterInfoRequest = new API.PatchCharacterInfoRequest();

    private async void OnEnable()
    {
        if (session == null)
        {
            logger.Log("Session is not assigned.", this, Logging.LogType.Error);
            return;
        }

        _root = GetComponent<UIDocument>().rootVisualElement;
        var body = _root.Q<VisualElement>("Body");
        if (body == null)
        {
            logger.Log("Body not found in the UI Document.", this, Logging.LogType.Error);
            return;
        }

        // Containers
        var container = body.Q<VisualElement>("CharacterEditContainer");
        if (container == null)
        {
            logger.Log("CharacterEditContainer not found in the UI Document.", this, Logging.LogType.Error);
            return;
        }
        _characterContainer = container.Q<VisualElement>("Character");
        _cosmeticsContainer = container.Q<VisualElement>("Cosmetics");
        if (_characterContainer == null || _cosmeticsContainer == null)
        {
            logger.Log("Character or Cosmetics not found in the UI Document.", this, Logging.LogType.Error);
            return;
        }

        _characterPreview = _characterContainer.Q<VisualElement>("CharacterPreview");
        if (_characterPreview == null)
        {
            logger.Log("CharacterPreview not found in Character container.", this, Logging.LogType.Error);
            return;
        }

        var buttonsContainer = _cosmeticsContainer.Q<VisualElement>("Buttons");
        if (buttonsContainer == null)
        {
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

        if (_nameInput == null || _bioInput == null || _backButton == null || _cancelButton == null || _saveButton == null || _errorMessage == null)
        {
            logger.Log("Buttons or Inputs not found in UI Document.", this, Logging.LogType.Error);
            return;
        }

        if (session.IsFirstLogin)
        {
            logger.Log("First login detected, hiding back button.", this);
            _backButton.style.display = DisplayStyle.None;
        }

        characterInfoRequest.category_sprites = new Dictionary<string, string>();
        registerCallbacks(true);
        await fetchCharacterInfo();
        await fetchCategories();
    }

    private void OnDisable()
    {
        registerCallbacks(false);

        for (int i = 0; i < _categoriesList.contentContainer.childCount; i++)
        {
            var btn = _categoriesList.contentContainer[i] as Button;
            if (btn != null)
            {
                btn.clicked -= async () => await onCategoryClicked(btn.name, btn.text);
            }
        }
    }

    /// <summary>
    /// Registers button click callbacks.
    /// </summary>
    private void registerCallbacks(bool shouldRegister)
    {
        if (shouldRegister)
        {
            logger.Log("Registering button callbacks", this);
            _backButton.clicked += onBackClicked;
            _cancelButton.clicked += onCancelClicked;
            _saveButton.clicked += async () => await onSaveClicked();
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
        else
        {
            logger.Log("Unregistering button callbacks", this);
            _backButton.clicked -= onBackClicked;
            _cancelButton.clicked -= onCancelClicked;
            _saveButton.clicked -= async () => await onSaveClicked();
            _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
    }

    /* --- BUTTON HANDLERS --- */

    /// <summary>
    /// Handles back button click event to go back to homepage.
    /// </summary>
    private void onBackClicked()
    {
        logger.Log("Back Button Clicked", this);
        transform.parent.gameObject.SetActive(false);
    }

    /// <summary>
    /// Handles cancel button click event.
    /// </summary>
    private void onCancelClicked()
    {
        logger.Log("Cancel Button Clicked", this);
    }

    /// <summary>
    /// Handles save button click event to save character info.
    /// </summary>
    private async Task onSaveClicked()
    {
        logger.Log("Save Button Clicked", this);
        logger.Log($"Name: {_nameInput.value}, Bio {_bioInput.value}", this);

        _errorMessage.text = "";
        if (_nameInput.value == "")
        {
            _errorMessage.text = "Name cannot be empty.";
            return;
        }

        characterInfoRequest.character_name = _nameInput.value;
        characterInfoRequest.character_bio = _bioInput.value;

        await updateCharacterInfo();
    }

    /// <summary>
    /// Handles category click events and fetches sprites for that category.
    /// </summary>
    private async Task onCategoryClicked(string categoryId, string categoryName)
    {
        if (categoryId == _selectedCategoryId)
        {
            return;
        }
        logger.Log($"Category clicked: {categoryId}", this);
        _selectedCategoryId = categoryId;
        _selectedCategoryName = categoryName;

        await fetchSpritesByCategory(categoryId);
    }

    /// <summary>
    /// Handles item click events and changes the sprite.
    /// </summary>
    private void onItemClicked(Texture2D texture, string spriteId)
    {
        logger.Log($"Item clicked: {spriteId}", this);
        var category = spriteManager.GetPartCategoryFromCategoryName(_selectedCategoryName);
        spriteManager.ChangeSprite(category, texture);
        characterInfoRequest.category_sprites[_selectedCategoryId] = spriteId;
        _saveButton.text = "Save";
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        logger.Log("Geometry changed.", this);
        centerCharacterPreview();
    }

    /* --- CHARACTER INFO HANDLING --- */

    /// <summary>
    /// Updates the current character information to server.
    /// </summary>
    private async Task updateCharacterInfo()
    {
        var characterInfo = await playerService.PatchCharacterInfoAsync(characterInfoRequest);
        if (characterInfo != null)
        {
            logger.Log("Character info successfully updated", this);
            session.IsFirstLogin = false;
            session.CharacterName = characterInfo.character_name;
            _saveButton.text = "Saved";
        }
        else
        {
            logger.Log("Failed to update character info", this, Logging.LogType.Error);
            _errorMessage.text = "Failed to update character info";
        }
    }

    /// <summary>
    /// Fetches the current character information from the server.
    /// </summary>
    private async Task fetchCharacterInfo()
    {
        var characterInfo = await playerService.GetCharacterInfoAsync();
        if (characterInfo != null)
        {
            logger.Log("Character info successfully retrieved", this);
            _nameInput.value = characterInfo.character_name;
            _bioInput.value = characterInfo.character_bio;
            characterInfoRequest.category_sprites = characterInfo.category_sprites;
        }
        else
        {
            logger.Log("Failed to retrieve character info", this, Logging.LogType.Warning);
        }
    }

    /* --- CATEGORIES & ITEMS HANDLING --- */

    /// <summary>
    /// Fetches categories from the server and populates the categories list.
    /// </summary>
    private async Task fetchCategories()
    {
        var response = await assetsService.GetCategoriesAsync();
        if (response == null || response.category_list == null)
        {
            logger.Log("Failed to fetch categories", this, Logging.LogType.Error);
            _errorMessage.text = "Failed to load categories.";
            return;
        }

        populateCategories(response.category_list);
        await onCategoryClicked(response.category_list[0].category_id, response.category_list[0].category_name);
    }

    /// <summary>
    /// Fetches sprites for a given category from the server and populates the items list.
    /// </summary>
    private async Task fetchSpritesByCategory(string categoryId)
    {
        var response = await assetsService.GetSpritesByCategoryAsync(categoryId);
        if (response == null || response.sprites_list == null)
        {
            logger.Log("Failed to fetch sprites", this, Logging.LogType.Error);
            _errorMessage.text = "Failed to load sprites.";
            _itemsList.contentContainer.Clear();
            return;
        }

        populateItems(response.sprites_list);
    }

    /// <summary>
    /// Populates the categories list with buttons.
    /// </summary>
    private void populateCategories(API.SpriteCategoryResponse[] categories)
    {
        _categoriesList.contentContainer.Clear();

        foreach (var category in categories)
        {
            var btn = new Button();
            btn.AddToClassList("category_button");
            btn.text = category.category_name;
            btn.name = category.category_id;
            btn.clicked += async () => await onCategoryClicked(category.category_id, category.category_name);
            _categoriesList.contentContainer.Add(btn);

            loadCategoryIcon(category.category_id, btn); // Load icon else use text
        }
    }

    /// <summary>
    /// Populates the items list with sprite buttons.
    /// </summary>
    private async void populateItems(API.SpriteResponse[] sprites)
    {
        _itemsList.contentContainer.Clear();

        foreach (var sprite in sprites)
        {
            var btn = new Button();
            btn.AddToClassList("item_button");
            btn.name = sprite.sprite_id;

            _itemsList.contentContainer.Add(btn);
            var texture = await assetsService.DownloadTexture2D(sprite.sprite_id);
            if (texture != null)
            {
                btn.style.backgroundImage = new StyleBackground(texture);
                btn.text = "";
                btn.clicked += () => onItemClicked(texture, sprite.sprite_id);
            }
            else
            {
                btn.text = sprite.sprite_id;
                logger.Log($"Failed to load texture for sprite: {sprite.sprite_id}", this, Logging.LogType.Warning);
            }
        }
    }

    /// <summary>
    /// Loads the first sprite of a category as the category button icon.
    /// </summary>
    private async void loadCategoryIcon(string categoryId, Button categoryButton)
    {
        var response = await assetsService.GetSpritesByCategoryAsync(categoryId);
        if (response == null || response.sprites_list == null || response.sprites_list.Length == 0)
        {
            logger.Log($"No sprites available for category icon: {categoryId}", this, Logging.LogType.Warning);
            return;
        }

        // Get the first sprite as the category icon
        var firstSprite = response.sprites_list[0];
        var texture = await assetsService.DownloadTexture2D(firstSprite.sprite_id);
        if (texture != null)
        {
            categoryButton.style.backgroundImage = new StyleBackground(texture);
            categoryButton.text = "";
        }
        else
        {
            logger.Log($"Failed to load icon texture for category: {categoryId}", this, Logging.LogType.Warning);
        }
    }

    private void centerCharacterPreview()
    {
        if (_characterPreview == null || canvasCharacterPreview == null)
            return;

        var rect = _characterPreview.worldBound;

        // Get screen center of the UI Toolkit element (Toolkit origin = top-left)
        Vector2 screenCenter = new Vector2(rect.xMin + rect.width / 2f, rect.yMin + rect.height / 2f);

        // Convert Y from top-left to bottom-left origin
        screenCenter.y = Screen.height - screenCenter.y;

        // Convert screen point to local position within the canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasCharacterPreview.parent as RectTransform,
            screenCenter,
            null, // camera if Canvas = Screen Space - Overlay
            out Vector2 localPoint
        );

        canvasCharacterPreview.anchoredPosition = localPoint + characterInContainerOffset;
    }
}
