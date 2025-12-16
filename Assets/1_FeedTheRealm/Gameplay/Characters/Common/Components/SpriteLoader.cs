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

    private Dictionary<FacingDirection, Dictionary<CharacterPartSprite, Transform>> _cachedPartsPerDirections = new Dictionary<FacingDirection, Dictionary<CharacterPartSprite, Transform>>();

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
                var cachedParts = new Dictionary<CharacterPartSprite, Transform>();
                cachedParts[CharacterPartSprite.Hair] = FindChildRecursive(directionTransform, "Hair");
                cachedParts[CharacterPartSprite.Beard] = FindChildRecursive(directionTransform, "Beard");
                cachedParts[CharacterPartSprite.EyeBrows] = FindChildRecursive(directionTransform, "EyesBrows");
                cachedParts[CharacterPartSprite.Eyes] = FindChildRecursive(directionTransform, "Eyes");
                cachedParts[CharacterPartSprite.Mouth] = FindChildRecursive(directionTransform, "Mouth");

                cachedParts[CharacterPartSprite.ArmorBody] = directionTransform.Find("UpperBody")?.Find("Armor");
                cachedParts[CharacterPartSprite.ArmorHelmet] = FindChildRecursive(directionTransform, "Helmet");

                cachedParts[CharacterPartSprite.ArmorArmL] = FindChildRecursive(directionTransform, "ArmL")?.Find("Armor");
                cachedParts[CharacterPartSprite.ArmorSleeveL] = FindChildRecursive(directionTransform, "ArmL")?.Find("Sleeve");
                cachedParts[CharacterPartSprite.ArmorHandL] = FindChildRecursive(directionTransform, "HandL")?.Find("Armor");
                cachedParts[CharacterPartSprite.ArmorArmR] = FindChildRecursive(directionTransform, "ArmR")?.Find("Armor");
                cachedParts[CharacterPartSprite.ArmorSleeveR] = FindChildRecursive(directionTransform, "ArmR")?.Find("Sleeve");
                cachedParts[CharacterPartSprite.ArmorHandR] = FindChildRecursive(directionTransform, "HandR")?.Find("Armor");

                cachedParts[CharacterPartSprite.ArmorLegL] = FindChildRecursive(directionTransform, "LegL")?.Find("Armor");
                cachedParts[CharacterPartSprite.ArmorLegR] = FindChildRecursive(directionTransform, "LegR")?.Find("Armor");

                cachedParts[CharacterPartSprite.EarringL] = FindChildRecursive(directionTransform, "EarringL");
                cachedParts[CharacterPartSprite.EarringR] = FindChildRecursive(directionTransform, "EarringR");
                cachedParts[CharacterPartSprite.Back] = FindChildRecursive(directionTransform, "Back");
                cachedParts[CharacterPartSprite.Mask] = FindChildRecursive(directionTransform, "Mask");
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
    private void ReplacePartSprite(Sprite newSprite, FacingDirection direction, params CharacterPartSprite[] pathSegments) {
        if (pathSegments == null || pathSegments.Length == 0) {
            logger.Log("No path segments provided to ReplacePartSprite", this, Logging.LogType.Warning);
            return;
        }

        if (!_cachedPartsPerDirections.TryGetValue(direction, out Dictionary<CharacterPartSprite, Transform> partsDict)) {
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
            CharacterPartSprite category = spriteManager.GetSpritePartFromCategoryName(categoryName);
            if (category != CharacterPartSprite.None) {
                switch (category) {
                    case CharacterPartSprite.ArmorHelmet:
                        ChangeHelmet(entry.Value);
                        break;
                    case CharacterPartSprite.ArmorBody:
                        ChangeBody(entry.Value);
                        break;
                    case CharacterPartSprite.ArmorArmL:
                    case CharacterPartSprite.ArmorArmR:
                        ChangeArms(entry.Value);
                        break;
                    case CharacterPartSprite.ArmorLegL:
                    case CharacterPartSprite.ArmorLegR:
                        ChangeLegs(entry.Value);
                        break;
                    case CharacterPartSprite.ArmorHandL:
                    case CharacterPartSprite.ArmorHandR:
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
