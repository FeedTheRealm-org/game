using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FTR.UI;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterEditController
{
    private string assetsWorldId = string.Empty;
    private string assetsPlayerId = string.Empty;

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
                const string err = "Unhandled character part category";
                logger?.Log(err, this, Logging.LogType.Error);
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
        CloseEditorPopup();
    }

    /// <summary>
    /// Handles cancel button click event.
    /// </summary>
    private async void onCancelClicked()
    {
        logger.Log("Cancel Button Clicked", this);

        await ResetCharacterSpritesToInitialState();

        if (spritesOnlyEditorMode && closeOnCancelInEditorMode)
        {
            CloseEditorPopup();
        }
    }

    /// <summary>
    /// Handles save button click event to save character info.
    /// </summary>
    private async Task onSaveClicked(GameObject confirmPopupPrefab)
    {
        if (confirmPopupPrefab == null)
        {
            logger?.Log("Confirm dialog prefab reference is missing.", this, Logging.LogType.Error);
            return;
        }
        var confirmPopup = Instantiate(confirmPopupPrefab);
        var dialogController = confirmPopup.GetComponent<ConfirmPopupController>();
        if (dialogController == null)
        {
            logger?.Log(
                "Confirm dialog prefab does not contain an ConfirmPopupController implementation.",
                this,
                Logging.LogType.Error
            );
            Destroy(confirmPopup);
            return;
        }
        dialogController.Show(
            title: "Save Changes",
            question: $"Are you sure you want to save these changes?",
            onConfirm: async () =>
            {
                var nameValue = _nameInput?.value ?? characterInfoRequest.character_name;
                var bioValue = _bioInput?.value ?? characterInfoRequest.character_bio;
                logger.Log($"Name: {nameValue}, Bio {bioValue}", this);

                if (!spritesOnlyEditorMode && string.IsNullOrWhiteSpace(nameValue))
                {
                    ShowToastError("Name cannot be empty.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(nameValue))
                {
                    nameValue = "EditorCharacter";
                }

                characterInfoRequest.character_name = nameValue;
                characterInfoRequest.character_bio = bioValue ?? string.Empty;
                EnsureCharacterColorFields();

                if (characterInfoRepository != null)
                {
                    await saveCharacterWithPersistenceStrategy();
                    return;
                }

                await updateCharacterInfo();
            },
            onCancel: () => { }
        );
    }

    /// <summary>
    /// Handles category button click events.
    /// </summary>
    private async void OnCategoryButtonClicked(Button btn)
    {
        // Handle color category button (hardcoded in UXML, not from server)
        if (btn.name == ColorCategoryName)
        {
            logger.Log($"Color category button clicked: {btn.name}", this);
            await onCategoryClicked("COLOR_TAB", ColorCategoryName);
            return;
        }

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
            UpdateCategorySelectionVisual();
            UpdateColorControlsVisibility();

            if (IsColorCategorySelected())
            {
                ClearItems();
                UpdatePaginationControls(0, 0);
                return;
            }

            if (_itemsList.contentContainer.childCount <= 1)
            {
                await fetchSpritesByCategory(categoryId);
            }
            return;
        }
        logger.Log($"Category clicked: {categoryId}", this);
        _selectedCategoryId = categoryId;
        _selectedCategoryName = categoryName;
        UpdateCategorySelectionVisual();
        UpdateColorControlsVisibility();

        if (IsColorCategorySelected())
        {
            ClearItems();
            UpdatePaginationControls(0, 0);
            return;
        }

        _currentCosmeticsOffset = 0;
        _currentCosmeticsTotalCount = 0;
        _hasNextCosmeticsPage = false;

        await fetchSpritesByCategory(categoryId);
    }

    private async void onPrevPageClicked()
    {
        if (_currentCosmeticsOffset <= 0)
            return;

        _currentCosmeticsOffset = Mathf.Max(0, _currentCosmeticsOffset - cosmeticsPageLimit);
        await fetchSpritesByCategory(_selectedCategoryId);
    }

    private async void onNextPageClicked()
    {
        if (!_hasNextCosmeticsPage)
            return;

        _currentCosmeticsOffset += cosmeticsPageLimit;
        await fetchSpritesByCategory(_selectedCategoryId);
    }

    /// <summary>
    /// Handles item click events and changes the sprite.
    /// </summary>
    private void onItemClicked(Texture2D texture, string spriteId)
    {
        logger.Log($"Item clicked: {spriteId}", this);
        var category = spriteManager.GetPartCategoryFromCategoryName(_selectedCategoryName);
        spriteManager.ChangeSprite(category, texture);

        if (characterInfoRequest.category_sprites == null)
        {
            characterInfoRequest.category_sprites = new Dictionary<string, string>();
        }

        TrackPreviewSelection(category, spriteId);
        characterInfoRequest.category_sprites[_selectedCategoryId] = spriteId;
        _saveButton.text = "Save";
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

            if (characterInfo.category_sprites != null)
            {
                characterInfoRequest.category_sprites = new Dictionary<string, string>(
                    characterInfo.category_sprites
                );
            }

            applyCharacterColorsFromResponse(characterInfo);
            CaptureInitialSpriteSnapshot();

            if (session != null)
            {
                session.IsFirstLogin = false;
                session.CharacterName = characterInfo.character_name;
            }
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
        // In editor flow, wait for SetupWithCharacterInfo() before loading any data.
        if (!loadFromSession && !spritesOnlyEditorMode)
        {
            logger.Log("Skipping auto-load until editor setup provides character data.", this);
            return;
        }

        if (characterInfoRepository != null)
        {
            var characterId = ResolveActiveCharacterId();
            if (string.IsNullOrEmpty(characterId))
            {
                logger.Log(
                    "Character id is missing, cannot load character info.",
                    this,
                    Logging.LogType.Warning
                );
                return;
            }

            var data = await characterInfoRepository.LoadAsync(characterId);
            if (data == null)
            {
                logger.Log("Failed to retrieve character info", this, Logging.LogType.Warning);
                return;
            }

            applyPersistenceData(data);
            return;
        }

        var characterInfo = await playerService.GetCharacterInfoAsync();
        if (characterInfo != null)
        {
            logger.Log("Character info successfully retrieved", this);
            _nameInput.value = characterInfo.character_name;
            _bioInput.value = characterInfo.character_bio;
            if (characterInfo.category_sprites != null)
            {
                characterInfoRequest.category_sprites = new Dictionary<string, string>(
                    characterInfo.category_sprites
                );
            }

            applyCharacterColorsFromResponse(characterInfo);
            CaptureInitialSpriteSnapshot();
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
        ClearAppliedCharacterSprites();
        _previewSpriteByPart.Clear();

        if (characterInfoRequest?.category_sprites == null)
        {
            ApplyAllCharacterColors();
            return;
        }

        foreach (var kvp in characterInfoRequest.category_sprites)
        {
            string spriteId = kvp.Value;
            if (string.IsNullOrEmpty(spriteId))
                continue;

            CharacterPartCategory part = CharacterPartCategory.None;

            if (System.Enum.TryParse<CharacterPartCategory>(kvp.Key, true, out var parsedPart))
            {
                part = parsedPart;
            }
            else if (_categories != null)
            {
                var category = _categories.FirstOrDefault(c => c.category_id == kvp.Key);
                if (category != null)
                {
                    part = spriteManager.GetPartCategoryFromCategoryName(category.category_name);
                }
            }

            if (part == CharacterPartCategory.None)
                continue;

            if (
                System.IO.Path.IsPathRooted(spriteId)
                || spriteId.StartsWith("file://")
                || spriteId.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)
            )
            {
                string loadPath = spriteId;
                if (!System.IO.Path.IsPathRooted(spriteId) && !spriteId.StartsWith("file://"))
                {
                    loadPath = System.IO.Path.GetFullPath(
                        System.IO.Path.Combine(Application.streamingAssetsPath, "Sprites", spriteId)
                    );
                }

                ApplyLocalSpriteOverride(part, loadPath);
                continue;
            }

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
                TrackPreviewSelection(part, spriteId);
            }
        }

        ApplyAllCharacterColors();
    }

    /// <summary>
    /// Exclusive function for offline/creator tools to bypass API restrictions.
    /// Manually loads a local PNG into the given character part.
    /// </summary>
    public bool ApplyLocalSpriteOverride(CharacterPartCategory part, string localFilePath)
    {
        if (string.IsNullOrEmpty(localFilePath) || !System.IO.File.Exists(localFilePath))
            return false;

        try
        {
            byte[] fileData = System.IO.File.ReadAllBytes(localFilePath);
            Texture2D tex = new Texture2D(2, 2);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            if (tex.LoadImage(fileData) && spriteManager != null)
            {
                spriteManager.ChangeSprite(part, tex);
                return true;
            }
            return false;
        }
        catch (System.Exception ex)
        {
            logger?.Log(
                $"Failed to apply local override: {ex.Message}",
                this,
                Logging.LogType.Error
            );
            return false;
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
            logger?.Log("Failed to fetch categories", this, Logging.LogType.Error);
            ShowToastError("Please Log in to load character customization options.");
            return;
        }

        _categories = response.category_list;
        UpdateCategorySelectionVisual();

        if (response.category_list.Length == 0)
        {
            logger?.Log("No categories returned from server", this, Logging.LogType.Warning);
            _selectedCategoryId = string.Empty;
            _selectedCategoryName = string.Empty;
            ClearItems();
            UpdatePaginationControls(0, 0);
            return;
        }

        foreach (var category in response.category_list)
        {
            var btn = _categoriesList.Q<Button>(category.category_name);
            if (btn == null)
            {
                logger?.Log(
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
        var colorBtn = _categoriesList.Q<Button>(ColorCategoryName);
        if (colorBtn != null)
        {
            System.Action colorAction = () => OnCategoryButtonClicked(colorBtn);
            colorBtn.clicked += colorAction;
            categoryButtonActions[colorBtn] = colorAction;
        }
        logger?.Log("Categories successfully populated", this);
        await onCategoryClicked(
            response.category_list[0].category_id,
            response.category_list[0].category_name
        );
        logger?.Log("First category auto-selected", this);
    }

    /// <summary>
    /// Fetches sprites for a given category from the server and populates the items list.
    /// </summary>
    private async Task fetchSpritesByCategory(string categoryId)
    {
        var requestVersion = ++_spritesRequestVersion;

        ReleaseCurrentPageTexturesExceptPinned();

        if (string.IsNullOrEmpty(categoryId))
        {
            ClearItems();
            _currentCosmeticsTotalCount = 0;
            UpdatePaginationControls(0, 0);
            return;
        }

        ClearItems();

        var response = await assetsService.GetSpritesByCategoryAsync(
            categoryId,
            _currentCosmeticsOffset,
            cosmeticsPageLimit,
            assetsWorldId,
            assetsPlayerId
        );

        if (!IsSpritesRequestCurrent(requestVersion, categoryId))
        {
            return;
        }

        if (response == null || response.sprites_list == null)
        {
            logger?.Log("Failed to fetch sprites", this, Logging.LogType.Error);
            ShowToastError("Failed to load sprites.");
            ClearItems();
            _currentCosmeticsTotalCount = 0;
            _hasNextCosmeticsPage = false;
            UpdatePaginationControls(0, 0);
            return;
        }

        _currentCosmeticsTotalCount = Mathf.Max(0, response.total_count);
        _hasNextCosmeticsPage =
            (_currentCosmeticsOffset + response.sprites_list.Length) < _currentCosmeticsTotalCount;

        if (response.sprites_list.Length == 0 && _currentCosmeticsOffset > 0)
        {
            _currentCosmeticsOffset = Mathf.Max(0, _currentCosmeticsOffset - cosmeticsPageLimit);
            _hasNextCosmeticsPage = false;
            UpdatePaginationControls(0, _currentCosmeticsTotalCount);
            await fetchSpritesByCategory(categoryId);
            return;
        }

        await populateItems(response.sprites_list, requestVersion, categoryId);

        if (!IsSpritesRequestCurrent(requestVersion, categoryId))
        {
            return;
        }

        UpdatePaginationControls(response.sprites_list.Length, _currentCosmeticsTotalCount);
    }

    /// <summary>
    /// Populates the items list with sprite buttons.
    /// </summary>
    private async Task populateItems(
        API.SpriteResponse[] sprites,
        int requestVersion,
        string requestCategoryId
    )
    {
        if (!IsSpritesRequestCurrent(requestVersion, requestCategoryId))
        {
            return;
        }

        ClearItems();

        foreach (var sprite in sprites)
        {
            if (!IsSpritesRequestCurrent(requestVersion, requestCategoryId))
            {
                return;
            }

            var btn = new Button();
            btn.AddToClassList("item_button");
            btn.name = sprite.sprite_id;
            Texture2D texture = null;
            var hasCachedTexture = textureCache.TryGetValue(sprite.sprite_id, out texture);
            if (!hasCachedTexture)
            {
                texture = await assetsService.DownloadTexture2D(sprite.sprite_id);

                if (!IsSpritesRequestCurrent(requestVersion, requestCategoryId))
                {
                    if (texture != null)
                    {
                        Destroy(texture);
                    }
                    return;
                }

                if (texture != null)
                {
                    textureCache[sprite.sprite_id] = texture;
                }
            }
            if (texture != null)
            {
                _currentPageTextureKeys.Add(sprite.sprite_id);

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
                logger?.Log(
                    $"Failed to load texture for sprite: {sprite.sprite_id}",
                    this,
                    Logging.LogType.Warning
                );
            }

            _itemsList.contentContainer.Add(btn);
        }
    }

    private bool IsSpritesRequestCurrent(int requestVersion, string requestCategoryId)
    {
        if (this == null)
            return false;

        bool isControllerActive;
        try
        {
            isControllerActive = isActiveAndEnabled;
        }
        catch (MissingReferenceException)
        {
            return false;
        }

        return isControllerActive
            && requestVersion == _spritesRequestVersion
            && requestCategoryId == _selectedCategoryId;
    }

    private void TrackPreviewSelection(CharacterPartCategory category, string spriteId)
    {
        if (string.IsNullOrEmpty(spriteId))
        {
            _previewSpriteByPart.Remove(category);
            return;
        }

        _previewSpriteByPart[category] = spriteId;
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

    private void UpdateCategorySelectionVisual()
    {
        if (_categories == null || _categoriesList == null)
            return;

        foreach (var category in _categories)
        {
            var btn = _categoriesList.Q<Button>(category.category_name);
            if (btn == null)
                continue;

            if (category.category_id == _selectedCategoryId)
            {
                btn.AddToClassList(SelectedCategoryClass);
            }
            else
            {
                btn.RemoveFromClassList(SelectedCategoryClass);
            }
        }
    }

    private void ReleaseCurrentPageTexturesExceptPinned()
    {
        if (_currentPageTextureKeys.Count == 0)
            return;

        var pinnedKeys = new HashSet<string>();
        if (characterInfoRequest?.category_sprites != null)
        {
            foreach (var spriteId in characterInfoRequest.category_sprites.Values)
            {
                if (!string.IsNullOrEmpty(spriteId))
                {
                    pinnedKeys.Add(spriteId);
                }
            }
        }

        foreach (var spriteId in _previewSpriteByPart.Values)
        {
            if (!string.IsNullOrEmpty(spriteId))
            {
                pinnedKeys.Add(spriteId);
            }
        }

        foreach (var key in _currentPageTextureKeys)
        {
            if (string.IsNullOrEmpty(key) || pinnedKeys.Contains(key))
                continue;

            if (textureCache.TryGetValue(key, out var texture))
            {
                if (texture != null)
                {
                    Destroy(texture);
                }

                textureCache.Remove(key);
            }
        }

        _currentPageTextureKeys.Clear();
    }

    private void UpdatePaginationControls(int loadedItemsCount, int totalItemsCount)
    {
        var safeLimit = Mathf.Max(1, cosmeticsPageLimit);
        var totalPages = Mathf.Max(1, Mathf.CeilToInt(totalItemsCount / (float)safeLimit));
        var currentPage = Mathf.Clamp((_currentCosmeticsOffset / safeLimit) + 1, 1, totalPages);

        _pageInfoLabel.text = $"Page {currentPage} of {totalPages}";
        _prevPageButton.SetEnabled(_currentCosmeticsOffset > 0);
        _nextPageButton.SetEnabled(_hasNextCosmeticsPage);
    }

    private async Task saveCharacterWithPersistenceStrategy()
    {
        var characterId = ResolveActiveCharacterId();
        if (string.IsNullOrEmpty(characterId))
        {
            logger?.Log(
                "Character id is missing, cannot save character info.",
                this,
                Logging.LogType.Error
            );
            ShowToastError("Failed to update character info.");
            return;
        }

        var characterInfo = await characterInfoRepository.SaveAsync(
            characterId,
            characterInfoRequest
        );

        if (characterInfo != null)
        {
            _saveButton.text = "Saved";

            if (characterInfo.category_sprites != null)
            {
                characterInfoRequest.category_sprites = new Dictionary<string, string>(
                    characterInfo.category_sprites
                );
            }

            applyCharacterColorsFromResponse(characterInfo);
            CaptureInitialSpriteSnapshot();

            if (session != null)
            {
                session.IsFirstLogin = false;
                session.CharacterName = characterInfo.character_name;
            }

            ShowToastSuccess("Character updated successfully.");

            if (spritesOnlyEditorMode && closeOnSaveInEditorMode)
            {
                CloseEditorPopup();
            }

            return;
        }

        logger?.Log("Failed to save character info", this, Logging.LogType.Error);
        ShowToastError("Failed to update character info.");
    }

    private void applyPersistenceData(API.CharacterInfoResponse data)
    {
        _nameInput.value = data.character_name ?? string.Empty;
        _bioInput.value = data.character_bio ?? string.Empty;

        characterInfoRequest.character_name = _nameInput.value;
        characterInfoRequest.character_bio = _bioInput.value;
        characterInfoRequest.category_sprites =
            data.category_sprites != null
                ? new Dictionary<string, string>(data.category_sprites)
                : new Dictionary<string, string>();

        applyCharacterColorsFromResponse(data);
        CaptureInitialSpriteSnapshot();
    }

    private void CaptureInitialSpriteSnapshot()
    {
        initialCategorySprites =
            characterInfoRequest.category_sprites != null
                ? new Dictionary<string, string>(characterInfoRequest.category_sprites)
                : new Dictionary<string, string>();
        captureInitialColorSnapshot();
    }

    private static readonly CharacterPartCategory[] ResettableCharacterParts =
    {
        CharacterPartCategory.ArmorHelmet,
        CharacterPartCategory.ArmorBody,
        CharacterPartCategory.ArmorLegL,
        CharacterPartCategory.ArmorLegR,
        CharacterPartCategory.Hair,
        CharacterPartCategory.Beard,
        CharacterPartCategory.EyeBrows,
        CharacterPartCategory.Eyes,
        CharacterPartCategory.Mouth,
        CharacterPartCategory.Back,
        CharacterPartCategory.EarringR,
        CharacterPartCategory.EarringL,
        CharacterPartCategory.Mask,
    };

    private void ClearAppliedCharacterSprites()
    {
        foreach (var part in ResettableCharacterParts)
        {
            spriteManager.ChangeSprite(part, null);
        }
    }

    private async Task ResetCharacterSpritesToInitialState()
    {
        ClearAppliedCharacterSprites();

        characterInfoRequest.category_sprites = new Dictionary<string, string>(
            initialCategorySprites
        );
        resetColorsToInitialState();
        _previewSpriteByPart.Clear();
        await ApplyCurrentCharacterSprites();

        if (_saveButton != null)
            _saveButton.text = "Saved";
    }

    public void SetAssetsWorldId(string worldId)
    {
        assetsWorldId = worldId?.Trim() ?? string.Empty;
    }

    public void SetAssetsPlayerId(string playerId)
    {
        assetsPlayerId = playerId?.Trim() ?? string.Empty;
    }
}
