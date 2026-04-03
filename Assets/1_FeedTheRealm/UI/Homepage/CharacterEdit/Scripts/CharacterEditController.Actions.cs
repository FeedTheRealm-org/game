using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FTR.Core.Client.Enums;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterEditController
{
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

        if (canvasCharacterPreview != null)
        {
            canvasCharacterPreview.gameObject.SetActive(false);
        }

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
}
