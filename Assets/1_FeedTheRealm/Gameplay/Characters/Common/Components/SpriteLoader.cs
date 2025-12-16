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

    private Dictionary<FacingDirection, Dictionary<CharacterSpritePart, Transform>> _cachedPartsPerDirections = new Dictionary<FacingDirection, Dictionary<CharacterSpritePart, Transform>>();

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
                var cachedParts = new Dictionary<CharacterSpritePart, Transform>();
                cachedParts[CharacterSpritePart.Hair] = FindChildRecursive(directionTransform, "Hair");
                cachedParts[CharacterSpritePart.Beard] = FindChildRecursive(directionTransform, "Beard");
                cachedParts[CharacterSpritePart.EyeBrows] = FindChildRecursive(directionTransform, "EyesBrows");
                cachedParts[CharacterSpritePart.Eyes] = FindChildRecursive(directionTransform, "Eyes");
                cachedParts[CharacterSpritePart.Mouth] = FindChildRecursive(directionTransform, "Mouth");

                cachedParts[CharacterSpritePart.ArmorBody] = directionTransform.Find("UpperBody")?.Find("Armor");
                cachedParts[CharacterSpritePart.ArmorHelmet] = FindChildRecursive(directionTransform, "Helmet");

                cachedParts[CharacterSpritePart.ArmorArmL] = FindChildRecursive(directionTransform, "ArmL")?.Find("Armor");
                cachedParts[CharacterSpritePart.ArmorSleeveL] = FindChildRecursive(directionTransform, "ArmL")?.Find("Sleeve");
                cachedParts[CharacterSpritePart.ArmorHandL] = FindChildRecursive(directionTransform, "HandL")?.Find("Armor");
                cachedParts[CharacterSpritePart.ArmorArmR] = FindChildRecursive(directionTransform, "ArmR")?.Find("Armor");
                cachedParts[CharacterSpritePart.ArmorSleeveR] = FindChildRecursive(directionTransform, "ArmR")?.Find("Sleeve");
                cachedParts[CharacterSpritePart.ArmorHandR] = FindChildRecursive(directionTransform, "HandR")?.Find("Armor");

                cachedParts[CharacterSpritePart.ArmorLegL] = FindChildRecursive(directionTransform, "LegL")?.Find("Armor");
                cachedParts[CharacterSpritePart.ArmorLegR] = FindChildRecursive(directionTransform, "LegR")?.Find("Armor");

                cachedParts[CharacterSpritePart.EarringL] = FindChildRecursive(directionTransform, "EarringL");
                cachedParts[CharacterSpritePart.EarringR] = FindChildRecursive(directionTransform, "EarringR");
                cachedParts[CharacterSpritePart.Back] = FindChildRecursive(directionTransform, "Back");
                cachedParts[CharacterSpritePart.Mask] = FindChildRecursive(directionTransform, "Mask");
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
    private void ReplacePartSprite(Sprite newSprite, FacingDirection direction, params CharacterSpritePart[] pathSegments) {
        if (pathSegments == null || pathSegments.Length == 0) {
            logger.Log("No path segments provided to ReplacePartSprite", this, Logging.LogType.Warning);
            return;
        }

        if (!_cachedPartsPerDirections.TryGetValue(direction, out Dictionary<CharacterSpritePart, Transform> partsDict)) {
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

    /// <summary>
    /// Downloads a sprite from the AssetsService and creates a sprite.
    /// </summary>
    private void DownloadTexture2D(string spriteId, System.Action<Texture2D> callback) {
        StartCoroutine(assetsService.DownloadTexture2D(spriteId, (texture) => {
            if (texture == null) {
                logger.Log($"Failed to download texture2D with ID: {spriteId}", this, Logging.LogType.Error);
                return;
            }
            logger.Log($"Successfully downloaded texture2D with ID: {spriteId}", this, Logging.LogType.Info);
            callback(texture);
        }));
    }

    /* --- PART CHANGE HANDLERS --- */

    private void ChangeArmor(string spriteId) {
        DownloadTexture2D(spriteId, (texture) => {
            var builder = new SpriteConfigBuilder();
            var director = new SpriteConfigDirector(builder);
            var helmetConfigs = director.BuildArmorHelmetSpriteConfig();
            var bodyConfigs = director.BuildArmorBodySpriteConfig();
            var armsConfigs = director.BuildArmorArmsSpriteConfig();
            var legsConfigs = director.BuildArmorLegsSpriteConfig();
            var handsConfigs = director.BuildArmorHandsSpriteConfig();
            var confs = helmetConfigs
                .Concat(bodyConfigs)
                .Concat(armsConfigs)
                .Concat(legsConfigs)
                .Concat(handsConfigs);
            foreach (var config in confs) {
                var newSprite = Sprite.Create(
                    texture,
                    config.Rect,
                    config.Pivot,
                    config.PixelsPerUnit
                );
                ReplacePartSprite(newSprite, config.Direction, config.Part);
            }
        });
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
                    case CharacterPartCategory.Armor:
                        ChangeArmor(entry.Value);
                        break;
                }
            }
        }
    }
}
