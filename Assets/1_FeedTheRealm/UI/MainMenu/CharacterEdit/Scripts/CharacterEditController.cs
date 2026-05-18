using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

/// <summary>
/// Manages the character editing interface and interactions.
/// </summary>
public partial class CharacterEditController : MonoBehaviour
{
    [Header("Session settings")]
    [SerializeField]
    private API.PlayerService playerService;

    [SerializeField]
    private API.AssetsService assetsService;

    [SerializeField]
    private Session.Session session;

    [SerializeField]
    private GameObject confirmPopupPrefab;

    [SerializeField]
    private bool loadFromSession = true;

    [SerializeField]
    private SpriteLoader spriteLoader;

    [SerializeField]
    private SpriteManager spriteManager;

    [SerializeField]
    private RectTransform canvasCharacterPreview;

    [SerializeField]
    private Camera characterPreviewCameraOverride;

    [SerializeField]
    private Vector2 characterInContainerOffset = new Vector2(-12, 0);

    [Inject]
    private CharacterInfoRepository characterInfoRepository;

    private ICharacterIdSource activeCharacterIdSource;
    private string activeCharacterId = string.Empty;
    private bool spritesOnlyEditorMode;
    private bool closeOnSaveInEditorMode;
    private bool closeOnCancelInEditorMode;

    public bool DisableApiFetches { get; set; }

    // Code-driven tuning values: edit these defaults directly in code.
    private float characterPreviewFillRatio = 0.82f;

    // Number of cosmetics loaded per request/page.
    private int cosmeticsPageLimit = 24;

    // Bigger orthographic size means the character appears smaller in preview.
    private float characterPreviewOrthographicSize = 1.2f;

    private float _lastAppliedPreviewFillRatio = -1f;
    private float _lastAppliedPreviewOrthoSize = -1f;
    private Camera _characterPreviewCamera;
    private int _lastScreenWidth = -1;
    private int _lastScreenHeight = -1;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    private SpriteConfigBuilder builder;
    private SpriteConfigDirector director;

    // Texture cache
    private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
    private HashSet<string> _currentPageTextureKeys = new HashSet<string>();
    private Dictionary<CharacterPartCategory, string> _previewSpriteByPart =
        new Dictionary<CharacterPartCategory, string>();
    private int _spritesRequestVersion;

    private const string SelectedCategoryClass = "category_button_selected";

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
    private Button _prevPageButton;
    private Button _nextPageButton;
    private Label _pageInfoLabel;

    private Label _errorMessage;

    // Inputs
    private TextField _nameInput;
    private TextField _bioInput;

    // Buttons
    private Button _emptyItemButton;
    private Button _backButton;
    private Button _cancelButton;
    private Button _saveButton;

    private System.Action _onEmptyItemClickedAction;
    private System.Action _onSaveClickedAction;
    private System.Action _onPrevPageClickedAction;
    private System.Action _onNextPageClickedAction;

    // Data
    private string _selectedCategoryId = "";
    private string _selectedCategoryName = "";
    private API.PatchCharacterInfoRequest characterInfoRequest =
        new API.PatchCharacterInfoRequest();
    private Dictionary<string, string> initialCategorySprites = new Dictionary<string, string>();
    private API.SpriteCategoryResponse[] _categories;
    private int _currentCosmeticsOffset;
    private int _currentCosmeticsTotalCount;
    private bool _hasNextCosmeticsPage;

    private async void OnEnable()
    {
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
        _prevPageButton = _cosmeticsContainer.Q<Button>("PrevPage");
        _nextPageButton = _cosmeticsContainer.Q<Button>("NextPage");
        _pageInfoLabel = _cosmeticsContainer.Q<Label>("PageInfo");

        initializeColorControls();
        _errorMessage.style.display = DisplayStyle.None;

        _emptyItemButton = _itemsList.Q<Button>("Empty");
        if (_emptyItemButton == null)
        {
            logger.Log("Empty item button not found in Items list.", this, Logging.LogType.Error);
            return;
        }

        _onEmptyItemClickedAction ??= () => onItemClicked(null, "");
        _onSaveClickedAction ??= () => _ = onSaveClicked(confirmPopupPrefab);
        _onPrevPageClickedAction ??= () => onPrevPageClicked();
        _onNextPageClickedAction ??= () => onNextPageClicked();

        cacheCharacterPreviewCamera();
        applyCharacterPreviewCameraZoom();
        logger.Log(
            $"Preview tuning applied: fill={characterPreviewFillRatio}, ortho={characterPreviewOrthographicSize}, cameraFound={(_characterPreviewCamera != null)}",
            this
        );

        if (canvasCharacterPreview != null)
        {
            canvasCharacterPreview.gameObject.SetActive(true);
            canvasCharacterPreview.localScale = Vector3.one;
            canvasCharacterPreview.anchorMin = new Vector2(0.5f, 0.5f);
            canvasCharacterPreview.anchorMax = new Vector2(0.5f, 0.5f);
            canvasCharacterPreview.pivot = new Vector2(0.5f, 0.5f);

            var previewParent = canvasCharacterPreview.parent as RectTransform;
            if (previewParent != null)
            {
                previewParent.localScale = Vector3.one;
            }
        }

        applyPersistencePresentation();
        applyEditorModePresentation();

        if (loadFromSession)
        {
            activeCharacterIdSource = new SessionUserIdSource(session);
            spriteManager.Initialize(
                spriteLoader,
                characterInfoRepository,
                activeCharacterIdSource
            );
        }

        _spritesRequestVersion++;
        _selectedCategoryId = string.Empty;
        _selectedCategoryName = string.Empty;
        _categories = null;
        _currentPageTextureKeys.Clear();
        initialCategorySprites = new Dictionary<string, string>();

        if (characterInfoRequest.category_sprites == null)
            characterInfoRequest.category_sprites = new Dictionary<string, string>();
        EnsureCharacterColorFields();

        _currentCosmeticsOffset = 0;
        _currentCosmeticsTotalCount = 0;
        _hasNextCosmeticsPage = false;
        registerCallbacks(true);
        UpdatePaginationControls(0, 0);
        _lastAppliedPreviewFillRatio = characterPreviewFillRatio;
        _lastAppliedPreviewOrthoSize = characterPreviewOrthographicSize;
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
        centerCharacterPreview();

        await System.Threading.Tasks.Task.Yield();

        if (!DisableApiFetches)
        {
            await fetchCharacterInfo();
            await fetchCategories();
        }
        await ApplyCurrentCharacterSprites();
    }

    public void SetupWithCharacterInfo(
        API.CharacterInfoResponse characterInfo,
        Action<API.CharacterInfoResponse> onSaveCallback
    )
    {
        var safeCharacterInfo = characterInfo ?? new API.CharacterInfoResponse();

        spritesOnlyEditorMode = true;
        closeOnSaveInEditorMode = true;
        closeOnCancelInEditorMode = true;
        loadFromSession = false;

        characterInfoRepository = new EditorNpcInfoRepository(safeCharacterInfo, onSaveCallback);

        activeCharacterId = !string.IsNullOrWhiteSpace(safeCharacterInfo.character_name)
            ? safeCharacterInfo.character_name
            : Guid.NewGuid().ToString();

        activeCharacterIdSource = new StaticCharacterIdSource(activeCharacterId);

        characterInfoRequest.character_name = safeCharacterInfo.character_name ?? string.Empty;
        characterInfoRequest.character_bio = safeCharacterInfo.character_bio ?? string.Empty;
        characterInfoRequest.skin_color = CloneColorOrDefault(safeCharacterInfo.skin_color);
        characterInfoRequest.hair_color = CloneColorOrDefault(safeCharacterInfo.hair_color);
        characterInfoRequest.eye_color = CloneColorOrDefault(safeCharacterInfo.eye_color);
        characterInfoRequest.category_sprites =
            safeCharacterInfo.category_sprites != null
                ? new Dictionary<string, string>(safeCharacterInfo.category_sprites)
                : new Dictionary<string, string>();
        initialCategorySprites = new Dictionary<string, string>(
            characterInfoRequest.category_sprites
        );
        captureInitialColorSnapshot();

        if (_nameInput != null)
        {
            _nameInput.value = characterInfoRequest.character_name;
        }

        if (_bioInput != null)
        {
            _bioInput.value = characterInfoRequest.character_bio;
        }

        applyEditorModePresentation();
        RefreshColorControlsFromRequest();

        spriteManager.Initialize(spriteLoader, characterInfoRepository, activeCharacterIdSource);
        ApplyAllCharacterColors();
        _ = ApplyCurrentCharacterSprites();
    }

    private void Update()
    {
        if (!isActiveAndEnabled || canvasCharacterPreview == null)
            return;

        if (_lastScreenWidth != Screen.width || _lastScreenHeight != Screen.height)
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            centerCharacterPreview();
        }

        if (!Mathf.Approximately(_lastAppliedPreviewOrthoSize, characterPreviewOrthographicSize))
        {
            _lastAppliedPreviewOrthoSize = characterPreviewOrthographicSize;
            applyCharacterPreviewCameraZoom();
        }

        if (!Mathf.Approximately(_lastAppliedPreviewFillRatio, characterPreviewFillRatio))
        {
            _lastAppliedPreviewFillRatio = characterPreviewFillRatio;
            centerCharacterPreview();
        }
    }

    private void cacheCharacterPreviewCamera()
    {
        if (_characterPreviewCamera != null)
            return;

        if (characterPreviewCameraOverride != null)
        {
            _characterPreviewCamera = characterPreviewCameraOverride;
            return;
        }

        var previewRoot = transform.parent != null ? transform.parent.Find("Preview") : null;
        if (previewRoot != null)
        {
            _characterPreviewCamera = previewRoot.GetComponentInChildren<Camera>(true);
        }
    }

    private void applyCharacterPreviewCameraZoom()
    {
        cacheCharacterPreviewCamera();

        if (_characterPreviewCamera == null)
        {
            logger.Log(
                "Character preview camera was not found. Ortho tuning is skipped.",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        if (_characterPreviewCamera.orthographic)
        {
            _characterPreviewCamera.orthographicSize = Mathf.Clamp(
                characterPreviewOrthographicSize,
                0.5f,
                8f
            );
        }
    }

    private void OnDisable()
    {
        _spritesRequestVersion++;
        registerCallbacks(false);

        foreach (var kvp in categoryButtonActions)
        {
            kvp.Key.clicked -= kvp.Value;
        }
        categoryButtonActions.Clear();

        ReleaseCurrentPageTexturesExceptPinned();
        ClearItems();

        if (canvasCharacterPreview != null)
        {
            canvasCharacterPreview.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
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
            _emptyItemButton.clicked += _onEmptyItemClickedAction;
            _backButton.clicked += onBackClicked;
            _cancelButton.clicked += onCancelClicked;
            _saveButton.clicked += _onSaveClickedAction;
            _prevPageButton.clicked += _onPrevPageClickedAction;
            _nextPageButton.clicked += _onNextPageClickedAction;
            registerColorControlCallbacks(true);
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
        else
        {
            logger.Log("Unregistering button callbacks", this);
            _emptyItemButton.clicked -= _onEmptyItemClickedAction;
            _backButton.clicked -= onBackClicked;
            _cancelButton.clicked -= onCancelClicked;
            _saveButton.clicked -= _onSaveClickedAction;
            _prevPageButton.clicked -= _onPrevPageClickedAction;
            _nextPageButton.clicked -= _onNextPageClickedAction;
            registerColorControlCallbacks(false);
            _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
    }

    private void applyPersistencePresentation()
    {
        if (_backButton == null)
        {
            return;
        }

        if (spritesOnlyEditorMode)
        {
            _backButton.style.display = DisplayStyle.None;
            return;
        }

        if (session != null && session.IsFirstLogin)
        {
            logger.Log("First login detected, hiding back button.", this);
            _backButton.style.display = DisplayStyle.None;
        }
        else
        {
            _backButton.style.display = DisplayStyle.Flex;
        }
    }

    private string ResolveActiveCharacterId()
    {
        if (!string.IsNullOrEmpty(activeCharacterId))
        {
            return activeCharacterId;
        }

        if (
            activeCharacterIdSource != null
            && !string.IsNullOrEmpty(activeCharacterIdSource.CharacterId)
        )
        {
            activeCharacterId = activeCharacterIdSource.CharacterId;
            return activeCharacterId;
        }

        if (session != null && !string.IsNullOrEmpty(session.UserID))
        {
            activeCharacterId = session.UserID;
            return activeCharacterId;
        }

        return string.Empty;
    }

    private void applyEditorModePresentation()
    {
        if (!spritesOnlyEditorMode)
        {
            return;
        }

        if (_nameInput != null)
        {
            _nameInput.isReadOnly = true;
            _nameInput.style.display = DisplayStyle.None;
        }

        if (_bioInput != null)
        {
            _bioInput.isReadOnly = true;
            _bioInput.style.display = DisplayStyle.None;
        }

        if (_backButton != null)
        {
            _backButton.style.display = DisplayStyle.None;
        }
    }

    private void CloseEditorPopup()
    {
        if (canvasCharacterPreview != null)
        {
            canvasCharacterPreview.gameObject.SetActive(false);
        }

        var container = transform.parent != null ? transform.parent.gameObject : gameObject;
        container.SetActive(false);
    }
}
