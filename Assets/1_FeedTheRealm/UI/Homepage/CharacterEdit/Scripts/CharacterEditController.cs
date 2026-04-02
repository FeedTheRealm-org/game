using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FTR.Core.Client.Enums;
using UnityEngine;
using UnityEngine.UIElements;

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

    private SpriteConfigBuilder builder;
    private SpriteConfigDirector director;

    // Texture cache
    private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

    // Category button actions for unregistering
    private Dictionary<Button, System.Action> categoryButtonActions =
        new Dictionary<Button, System.Action>();

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
    private Button _emptyItemButton;
    private Button _backButton;
    private Button _cancelButton;
    private Button _saveButton;

    // Data
    private string _selectedCategoryId = "";
    private string _selectedCategoryName = "";
    private API.PatchCharacterInfoRequest characterInfoRequest =
        new API.PatchCharacterInfoRequest();
    private API.SpriteCategoryResponse[] _categories;

    private async void OnEnable()
    {
        if (session == null)
        {
            logger.Log("Session is not assigned.", this, Logging.LogType.Error);
            return;
        }

        builder = new SpriteConfigBuilder();
        director = new SpriteConfigDirector(builder);

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
            logger.Log(
                "CharacterEditContainer not found in the UI Document.",
                this,
                Logging.LogType.Error
            );
            return;
        }
        _characterContainer = container.Q<VisualElement>("Character");
        _cosmeticsContainer = container.Q<VisualElement>("Cosmetics");
        if (_characterContainer == null || _cosmeticsContainer == null)
        {
            logger.Log(
                "Character or Cosmetics not found in the UI Document.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        _characterPreview = _characterContainer.Q<VisualElement>("CharacterPreview");
        if (_characterPreview == null)
        {
            logger.Log(
                "CharacterPreview not found in Character container.",
                this,
                Logging.LogType.Error
            );
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

        if (
            _nameInput == null
            || _bioInput == null
            || _backButton == null
            || _cancelButton == null
            || _saveButton == null
            || _errorMessage == null
        )
        {
            logger.Log("Buttons or Inputs not found in UI Document.", this, Logging.LogType.Error);
            return;
        }

        _errorMessage.style.display = DisplayStyle.None;

        _emptyItemButton = _itemsList.Q<Button>("Empty");
        if (_emptyItemButton == null)
        {
            logger.Log("Empty item button not found in Items list.", this, Logging.LogType.Error);
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
        await ApplyCurrentCharacterSprites();
    }

    private void OnDisable()
    {
        registerCallbacks(false);

        foreach (var kvp in categoryButtonActions)
        {
            kvp.Key.clicked -= kvp.Value;
        }
        categoryButtonActions.Clear();

        ClearItems();

        foreach (var texture in textureCache.Values)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }
        textureCache.Clear();
    }

    /// <summary>
    /// Registers button click callbacks.
    /// </summary>
    private void registerCallbacks(bool shouldRegister)
    {
        if (shouldRegister)
        {
            logger.Log("Registering button callbacks", this);
            _emptyItemButton.clicked += () => onItemClicked(null, "");
            _backButton.clicked += onBackClicked;
            _cancelButton.clicked += onCancelClicked;
            _saveButton.clicked += async () => await onSaveClicked();
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
        else
        {
            logger.Log("Unregistering button callbacks", this);
            _emptyItemButton.clicked -= () => onItemClicked(null, "");
            _backButton.clicked -= onBackClicked;
            _cancelButton.clicked -= onCancelClicked;
            _saveButton.clicked -= async () => await onSaveClicked();
            _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
    }

    private List<SpriteConfig> GetConfigsForPart(
        SpriteConfigDirector director,
        CharacterPartCategory part
    )
    {
        switch (part)
        {
            case CharacterPartCategory.ArmorHelmet:
                return director.BuildArmorHelmetSpriteConfig();
            case CharacterPartCategory.ArmorBody:
                return director.BuildArmorBodySpriteConfig();
            case CharacterPartCategory.ArmorLegR:
            case CharacterPartCategory.ArmorLegL:
                return director.BuildArmorLegsSpriteConfig();
            case CharacterPartCategory.Hair:
                return director.BuildHairSpriteConfig();
            case CharacterPartCategory.Beard:
                return director.BuildBeardSpriteConfig();
            case CharacterPartCategory.EyeBrows:
                return director.BuildEyeBrowsSpriteConfig();
            case CharacterPartCategory.Eyes:
                return director.BuildEyesSpriteConfig();
            case CharacterPartCategory.Mouth:
                return director.BuildMouthSpriteConfig();
            case CharacterPartCategory.Back:
                return director.BuildBackSpriteConfig();
            case CharacterPartCategory.EarringR:
            case CharacterPartCategory.EarringL:
                return director.BuildEarringsSpriteConfig();
            case CharacterPartCategory.Mask:
                return director.BuildMaskSpriteConfig();
            default:
                return null;
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

        if (string.IsNullOrWhiteSpace(_nameInput.value))
        {
            ShowToastError("Name cannot be empty.");
            return;
        }

        characterInfoRequest.character_name = _nameInput.value;
        characterInfoRequest.character_bio = _bioInput.value;

        await updateCharacterInfo();
    }

    /// <summary>
    /// Handles category button click events.
    /// </summary>
    private async void OnCategoryButtonClicked(Button btn)
    {
        var cat = _categories.First(c => c.category_name == btn.name);
        await onCategoryClicked(cat.category_id, cat.category_name);
    }

    /// <summary>
    /// Handles category click events and fetches sprites for that category.
    /// </summary>
    private async Task onCategoryClicked(string categoryId, string categoryName)
    {
        logger.Log($"onCategoryClicked called with ID: {categoryId}, Name: {categoryName}", this);
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
            ShowToastSuccess("Character updated successfully.");
        }
        else
        {
            logger.Log("Failed to update character info", this, Logging.LogType.Error);
            ShowToastError("Failed to update character info.");
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

    /// <summary>
    /// Applies the current character's equipped sprites to the preview.
    /// </summary>
    private async Task ApplyCurrentCharacterSprites()
    {
        if (_categories == null || characterInfoRequest.category_sprites == null)
            return;

        foreach (var kvp in characterInfoRequest.category_sprites)
        {
            var category = _categories.FirstOrDefault(c => c.category_id == kvp.Key);
            if (category == null)
                continue;

            string spriteId = kvp.Value;
            if (string.IsNullOrEmpty(spriteId))
                continue;

            var part = spriteManager.GetPartCategoryFromCategoryName(category.category_name);
            if (part == CharacterPartCategory.None)
                continue;

            Texture2D texture = null;
            if (!textureCache.TryGetValue(spriteId, out texture))
            {
                texture = await assetsService.DownloadTexture2D(spriteId);
                if (texture != null)
                {
                    textureCache[spriteId] = texture;
                }
            }
            if (texture != null)
            {
                spriteManager.ChangeSprite(part, texture);
            }
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
            ShowToastError("Failed to load categories.");
            return;
        }

        _categories = response.category_list;
        foreach (var category in response.category_list)
        {
            var btn = _categoriesList.Q<Button>(category.category_name);
            if (btn == null)
            {
                logger.Log(
                    $"Error: Category button {category.category_name} not found in UI.",
                    this,
                    Logging.LogType.Error
                );
                continue;
            }
            System.Action action = () => OnCategoryButtonClicked(btn);
            btn.clicked += action;
            categoryButtonActions[btn] = action;
        }
        logger.Log("Categories successfully populated", this);
        await onCategoryClicked(
            response.category_list[0].category_id,
            response.category_list[0].category_name
        );
        logger.Log("First category auto-selected", this);
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
            ShowToastError("Failed to load sprites.");
            ClearItems();
            return;
        }

        populateItems(response.sprites_list);
    }

    /// <summary>
    /// Populates the items list with sprite buttons.
    /// </summary>
    private async void populateItems(API.SpriteResponse[] sprites)
    {
        ClearItems();

        foreach (var sprite in sprites)
        {
            var btn = new Button();
            btn.AddToClassList("item_button");
            btn.name = sprite.sprite_id;

            _itemsList.contentContainer.Add(btn);
            Texture2D texture = null;
            if (!textureCache.TryGetValue(sprite.sprite_id, out texture))
            {
                texture = await assetsService.DownloadTexture2D(sprite.sprite_id);
                if (texture != null)
                {
                    textureCache[sprite.sprite_id] = texture;
                }
            }
            if (texture != null)
            {
                var category = spriteManager.GetPartCategoryFromCategoryName(_selectedCategoryName);
                var configs = GetConfigsForPart(director, category);
                if (configs != null && configs.Count > 0)
                {
                    var config = configs[0];
                    var spriteObj = Sprite.Create(
                        texture,
                        config.Rect,
                        config.Pivot,
                        config.PixelsPerUnit
                    );
                    btn.style.backgroundImage = new StyleBackground(spriteObj);
                }
                else
                {
                    btn.style.backgroundImage = new StyleBackground(texture);
                }
                btn.text = "";
                btn.clicked += () => onItemClicked(texture, sprite.sprite_id);
            }
            else
            {
                btn.text = sprite.sprite_id;
                logger.Log(
                    $"Failed to load texture for sprite: {sprite.sprite_id}",
                    this,
                    Logging.LogType.Warning
                );
            }
        }
    }

    /// <summary>
    /// Clears all items from the items list, except the first (empty).
    /// </summary>
    private void ClearItems()
    {
        while (_itemsList.contentContainer.childCount > 1)
        {
            _itemsList.contentContainer.RemoveAt(1);
        }
    }

    private void centerCharacterPreview()
    {
        if (_characterPreview == null || canvasCharacterPreview == null)
            return;

        var rect = _characterPreview.worldBound;

        // Get screen center of the UI Toolkit element (Toolkit origin = top-left)
        Vector2 screenCenter = new Vector2(
            rect.xMin + rect.width / 2f,
            rect.yMin + rect.height / 2f
        );

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

    private void ShowToastSuccess(string message)
    {
        ToastNotification.Show(message, "success", Color.green);
    }

    private void ShowToastError(string message)
    {
        ToastNotification.Show(message, "error", Color.red);
    }
}
