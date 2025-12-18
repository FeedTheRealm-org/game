using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public class SpriteLoader : MonoBehaviour {
    [SerializeField]
    private SpriteManager spriteManager; // Only used for character editor events

    [Header("Services settings")]
    [SerializeField]
    private API.AssetsService assetsService;

    [SerializeField]
    private API.PlayerService playerService;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    public string UserId { get; set; }

    private Dictionary<FacingDirection, Dictionary<CharacterPartCategory, Transform>> _cachedPartsPerDirections = new Dictionary<FacingDirection, Dictionary<CharacterPartCategory, Transform>>();
    private Dictionary<string, Texture2D> cachedCategoryTextures = new Dictionary<string, Texture2D>();

    private SpriteConfigBuilder builder;
    private SpriteConfigDirector director;

    private void Awake() {
        logger.Log("[SpriteLoader] Initializing sprites for character", this);

        builder = new SpriteConfigBuilder();
        director = new SpriteConfigDirector(builder);

        CachePartTransforms();
        if (spriteManager != null) {
            spriteManager.OnArmorHelmetChange += ChangeHelmet;
            spriteManager.OnArmorBodyChange += ChangeBody;
            spriteManager.OnArmorLegsChange += ChangeLegs;
        }
        _ = InitCharacterSpritesAsync();
    }

    private void OnDestroy() {
        if (spriteManager != null) {
            spriteManager.OnArmorHelmetChange -= ChangeHelmet;
            spriteManager.OnArmorBodyChange -= ChangeBody;
            spriteManager.OnArmorLegsChange -= ChangeLegs;
        }
    }

    public void StartLoadingSprites() {
        logger.Log($"[SpriteLoader] StartLoadingSprites called with UserId: '{UserId}'", this);
        _ = InitCharacterSpritesAsync();
    }

    private void CachePartTransforms() {
        foreach (FacingDirection direction in Enum.GetValues(typeof(FacingDirection))) {
            var directionTransform = FindChildRecursive(transform, direction.ToString());
            if (directionTransform != null) {
                var cachedParts = new Dictionary<CharacterPartCategory, Transform>();
                cachedParts[CharacterPartCategory.Hair] = FindChildRecursive(directionTransform, "Hair");
                cachedParts[CharacterPartCategory.Beard] = FindChildRecursive(directionTransform, "Beard");
                cachedParts[CharacterPartCategory.EyeBrows] = FindChildRecursive(directionTransform, "EyesBrows");
                cachedParts[CharacterPartCategory.Eyes] = FindChildRecursive(directionTransform, "Eyes");
                cachedParts[CharacterPartCategory.Mouth] = FindChildRecursive(directionTransform, "Mouth");

                cachedParts[CharacterPartCategory.ArmorBody] = directionTransform.Find("UpperBody")?.Find("Armor");
                cachedParts[CharacterPartCategory.ArmorHelmet] = FindChildRecursive(directionTransform, "Helmet");

                cachedParts[CharacterPartCategory.ArmorArmR] = FindChildRecursive(directionTransform, "ArmR")?.Find("Armor");
                cachedParts[CharacterPartCategory.ArmorArmL] = FindChildRecursive(directionTransform, "ArmL")?.Find("Armor");
                cachedParts[CharacterPartCategory.ArmorSleeveR] = FindChildRecursive(directionTransform, "ArmR")?.Find("Sleeve");
                cachedParts[CharacterPartCategory.ArmorSleeveL] = FindChildRecursive(directionTransform, "ArmL")?.Find("Sleeve");
                cachedParts[CharacterPartCategory.ArmorHandR] = FindChildRecursive(directionTransform, "HandR")?.Find("Armor");
                cachedParts[CharacterPartCategory.ArmorHandL] = FindChildRecursive(directionTransform, "HandL")?.Find("Armor");
                cachedParts[CharacterPartCategory.ArmorLegR] = FindChildRecursive(directionTransform, "LegR")?.Find("Armor");
                cachedParts[CharacterPartCategory.ArmorLegL] = FindChildRecursive(directionTransform, "LegL")?.Find("Armor");

                cachedParts[CharacterPartCategory.EarringR] = FindChildRecursive(directionTransform, "EarringR");
                cachedParts[CharacterPartCategory.EarringL] = FindChildRecursive(directionTransform, "EarringL");
                cachedParts[CharacterPartCategory.Back] = FindChildRecursive(directionTransform, "Back");
                cachedParts[CharacterPartCategory.Mask] = FindChildRecursive(directionTransform, "Mask");
                _cachedPartsPerDirections[direction] = cachedParts;
            } else {
                logger.Log($"Direction transform not found: {direction} under {gameObject.name}", this, Logging.LogType.Warning);
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string childName) {
        Transform result = parent.Find(childName);
        if (result != null) return result;

        foreach (Transform child in parent) {
            result = FindChildRecursive(child, childName);
            if (result != null) return result;
        }

        return null;
    }

    /// <summary>
    /// Replaces the sprite of a part at the given path.
    /// Example: ReplacePartSprite(newSprite, "Parent", "Child", "TargetObject")
    /// </summary>
    private void ReplacePartSprite(Sprite newSprite, FacingDirection direction, params CharacterPartCategory[] pathSegments) {
        if (pathSegments == null || pathSegments.Length == 0) {
            logger.Log("No path segments provided to ReplacePartSprite", this, Logging.LogType.Warning);
            return;
        }

        if (!_cachedPartsPerDirections.TryGetValue(direction, out Dictionary<CharacterPartCategory, Transform> partsDict)) {
            logger.Log($"Direction not cached: {direction} under {gameObject.name}", this, Logging.LogType.Warning);
            return;
        }

        if (!partsDict.TryGetValue(pathSegments[0], out Transform currentTransform) || currentTransform == null) {
            logger.Log($"Root child not found: {pathSegments[0]} under {gameObject.name}", this, Logging.LogType.Warning);
            return;
        }

        SpriteRenderer spriteRenderer = currentTransform.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) {
            logger.Log($"SpriteRenderer not found at path: {string.Join("/", pathSegments)}", this, Logging.LogType.Warning);
        }
        spriteRenderer.sprite = newSprite;
    }

    /* --- PART CHANGE HANDLERS --- */

    private void ChangeHelmet(Texture2D texture) {
        ChangeTexture(texture, director.BuildArmorHelmetSpriteConfig());
        logger.Log("[SpriteLoader] Changed Armor Helmet sprites", this);
    }

    private void ChangeBody(Texture2D texture) {
        ChangeTexture(texture, director.BuildArmorBodySpriteConfig());
        logger.Log("[SpriteLoader] Changed Armor Body sprites", this);
    }

    private void ChangeLegs(Texture2D texture) {
        ChangeTexture(texture, director.BuildArmorLegsSpriteConfig());
        logger.Log("[SpriteLoader] Changed Armor Legs sprites", this);
    }

    /// <summary>
    /// Changes the texture for multiple sprite configurations [or removes it if texture is null!].
    /// </summary>
    private void ChangeTexture(Texture2D texture, List<SpriteConfig> confs) {
        foreach (var config in confs) {
            Sprite sprite = null;

            if (texture != null) {
                sprite = Sprite.Create(
                    texture,
                    config.Rect,
                    config.Pivot,
                    config.PixelsPerUnit
                );
            }

            ReplacePartSprite(sprite, config.Direction, config.Part);
        }
    }

    /* --- INITIALIZATION UTILS --- */

    private async Task InitCharacterSpritesAsync() {
        // Fetch categories
        var categoriesResponse = await assetsService.GetCategoriesAsync();
        if (categoriesResponse == null) {
            logger.Log("Failed to fetch categories", this, Logging.LogType.Error);
            return;
        }

        // Fetch character info
        logger.Log($"[SpriteLoader] Fetching character info for UserId: '{UserId}'", this);
        var characterInfo = await playerService.GetCharacterInfoAsync(UserId);
        if (characterInfo == null) {
            logger.Log("Failed to fetch character info", this, Logging.LogType.Warning);
            return;
        }

        // All data fetched, apply sprites
        if (categoriesResponse.category_list == null) {
            logger.Log("No categories found in response.", this, Logging.LogType.Error);
            return;
        }

        Dictionary<string, string> existingCategories = new Dictionary<string, string>();
        foreach (var category in categoriesResponse.category_list) {
            existingCategories[category.category_id] = category.category_name;
        }

        if (characterInfo.category_sprites == null) {
            logger.Log("No character sprites found in response.", this, Logging.LogType.Warning);
            return;
        }

        foreach (var entry in characterInfo.category_sprites) {
            if (!cachedCategoryTextures.TryGetValue(entry.Value, out Texture2D texture)) {
                texture = await assetsService.DownloadTexture2D(entry.Value);
                if (texture == null) continue;
                cachedCategoryTextures[entry.Value] = texture;
            }

            string categoryName = existingCategories.TryGetValue(entry.Key, out var name) ? name : "";
            CharacterPartCategory category = spriteManager.GetPartCategoryFromCategoryName(categoryName);
            if (category != CharacterPartCategory.None) {
                switch (category) {
                    case CharacterPartCategory.ArmorHelmet:
                        ChangeHelmet(texture);
                        break;
                    case CharacterPartCategory.ArmorBody:
                        ChangeBody(texture);
                        break;
                    case CharacterPartCategory.ArmorLegL:
                    case CharacterPartCategory.ArmorLegR:
                        ChangeLegs(texture);
                        break;
                    default:
                        logger.Log($"No handler for category: {category} under {gameObject.name}", this, Logging.LogType.Warning);
                        break;
                }
            }
        }
    }
}
