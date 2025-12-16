using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpriteLoader : MonoBehaviour {
    [SerializeField]
    private SpriteManager spriteManager;

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

    private void CachePartTransforms() {
        var directions = new FacingDirection[] {
            FacingDirection.Front,
            FacingDirection.Back,
            FacingDirection.Right,
            FacingDirection.Left
        };

        foreach (var direction in directions) {
            var directionTransform = transform.Find(direction.ToString());
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

    public void StartLoadingSprites() {
        logger.Log($"[SpriteLoader] StartLoadingSprites called with UserId: '{UserId}'", this);
        StartCoroutine(InitCharacterSpritesCoroutine());
    }

    private void Awake() {
        logger.Log("[SpriteLoader] Initializing sprites for character", this);
        CachePartTransforms();
        if (spriteManager != null) {
        }
        StartCoroutine(InitCharacterSpritesCoroutine());
    }

    private void OnDestroy() {
        if (spriteManager != null) {
        }
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

    private async void ChangeHelmet(string spriteId) {
        if (!cachedCategoryTextures.TryGetValue(spriteId, out Texture2D texture)) {
            texture = await assetsService.DownloadTexture2D(spriteId);
            if (texture == null) return;
            cachedCategoryTextures[spriteId] = texture;
        }

        var builder = new SpriteConfigBuilder();
        var director = new SpriteConfigDirector(builder);

        var confs = director.BuildArmorHelmetSpriteConfig();

        foreach (var config in confs) {
            var newSprite = Sprite.Create(
                texture,
                config.Rect,
                config.Pivot,
                config.PixelsPerUnit
            );
            ReplacePartSprite(newSprite, config.Direction, config.Part);
        }
    }

    private async void ChangeBody(string spriteId) {
        if (!cachedCategoryTextures.TryGetValue(spriteId, out Texture2D texture)) {
            texture = await assetsService.DownloadTexture2D(spriteId);
            if (texture == null) return;
            cachedCategoryTextures[spriteId] = texture;
        }

        var builder = new SpriteConfigBuilder();
        var director = new SpriteConfigDirector(builder);

        var confs = director.BuildArmorBodySpriteConfig();

        foreach (var config in confs) {
            var newSprite = Sprite.Create(
                texture,
                config.Rect,
                config.Pivot,
                config.PixelsPerUnit
            );
            ReplacePartSprite(newSprite, config.Direction, config.Part);
        }
    }

    private async void ChangeArms(string spriteId) {
        if (!cachedCategoryTextures.TryGetValue(spriteId, out Texture2D texture)) {
            texture = await assetsService.DownloadTexture2D(spriteId);
            if (texture == null) return;
            cachedCategoryTextures[spriteId] = texture;
        }

        var builder = new SpriteConfigBuilder();
        var director = new SpriteConfigDirector(builder);

        var confs = director.BuildArmorArmsSpriteConfig();

        foreach (var config in confs) {
            var newSprite = Sprite.Create(
                texture,
                config.Rect,
                config.Pivot,
                config.PixelsPerUnit
            );
            ReplacePartSprite(newSprite, config.Direction, config.Part);
        }
    }

    private async void ChangeLegs(string spriteId) {
        if (!cachedCategoryTextures.TryGetValue(spriteId, out Texture2D texture)) {
            texture = await assetsService.DownloadTexture2D(spriteId);
            if (texture == null) return;
            cachedCategoryTextures[spriteId] = texture;
        }

        var builder = new SpriteConfigBuilder();
        var director = new SpriteConfigDirector(builder);

        var confs = director.BuildArmorLegsSpriteConfig();

        foreach (var config in confs) {
            var newSprite = Sprite.Create(
                texture,
                config.Rect,
                config.Pivot,
                config.PixelsPerUnit
            );
            ReplacePartSprite(newSprite, config.Direction, config.Part);
        }
    }

    private async void ChangeHands(string spriteId) {
        if (!cachedCategoryTextures.TryGetValue(spriteId, out Texture2D texture)) {
            texture = await assetsService.DownloadTexture2D(spriteId);
            if (texture == null) return;
            cachedCategoryTextures[spriteId] = texture;
        }

        var builder = new SpriteConfigBuilder();
        var director = new SpriteConfigDirector(builder);

        var confs = director.BuildArmorHandsSpriteConfig();

        foreach (var config in confs) {
            var newSprite = Sprite.Create(
                texture,
                config.Rect,
                config.Pivot,
                config.PixelsPerUnit
            );
            ReplacePartSprite(newSprite, config.Direction, config.Part);
        }
    }

    /* --- INITIALIZATION UTILS --- */
    private IEnumerator InitCharacterSpritesCoroutine() {
        // Fetch categories
        API.SpriteCategoryListResponse categoriesResponse = null;
        string categoriesError = null;
        yield return assetsService.GetCategories((response, err) => {
            categoriesResponse = response;
            categoriesError = err;
        });

        if (!string.IsNullOrEmpty(categoriesError)) {
            logger.Log($"Failed to fetch categories: {categoriesError}", this, Logging.LogType.Error);
            yield break;
        }

        // Fetch character info
        API.CharacterInfoResponse characterInfo = null;
        string characterError = null;
        logger.Log($"[SpriteLoader] Fetching character info for UserId: '{UserId}'", this);
        yield return playerService.GetCharacterInfo((info, err) => {
            characterInfo = info;
            characterError = err;
        }, UserId);

        if (!string.IsNullOrEmpty(characterError)) {
            logger.Log($"Failed to fetch character info: {characterError}", this, Logging.LogType.Warning);
            yield break;
        }

        // All data fetched, apply sprites
        if (categoriesResponse?.category_list == null) {
            logger.Log("No categories found in response.", this, Logging.LogType.Error);
            yield break;
        }

        Dictionary<string, string> existingCategories = new Dictionary<string, string>();
        foreach (var category in categoriesResponse.category_list) {
            existingCategories[category.category_id] = category.category_name;
        }

        if (characterInfo?.category_sprites == null) {
            logger.Log("No character sprites found in response.", this, Logging.LogType.Warning);
            yield break;
        }

        foreach (var entry in characterInfo.category_sprites) {
            string categoryName = existingCategories.TryGetValue(entry.Key, out var name) ? name : "";
            CharacterPartCategory category = spriteManager.GetPartCategoryFromCategoryName(categoryName);
            if (category != CharacterPartCategory.None) {
                switch (category) {
                    case CharacterPartCategory.ArmorHelmet:
                        ChangeHelmet(entry.Value);
                        break;
                    case CharacterPartCategory.ArmorBody:
                        ChangeBody(entry.Value);
                        break;
                    case CharacterPartCategory.ArmorArmL:
                    case CharacterPartCategory.ArmorArmR:
                        ChangeArms(entry.Value);
                        break;
                    case CharacterPartCategory.ArmorLegL:
                    case CharacterPartCategory.ArmorLegR:
                        ChangeLegs(entry.Value);
                        break;
                    case CharacterPartCategory.ArmorHandL:
                    case CharacterPartCategory.ArmorHandR:
                        ChangeHands(entry.Value);
                        break;
                    default:
                        logger.Log($"No handler for category: {category} under {gameObject.name}", this, Logging.LogType.Warning);
                        break;
                }
            }
        }
    }
}
