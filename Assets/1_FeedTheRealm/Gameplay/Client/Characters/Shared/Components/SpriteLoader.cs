using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FTR.Core.Client.Enums;
using UnityEngine;

public class SpriteLoader : MonoBehaviour
{
    public static event Action<SpriteLoader> OnSpriteLoaderReady;

    [SerializeField]
    private SpriteManager spriteManager; // Only used for character editor events

    [Header("Services settings")]
    [SerializeField]
    private API.AssetsService assetsService;

    [SerializeField]
    private API.PlayerService playerService;

    [SerializeField]
    private API.ItemAssetsService itemAssetsService;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    public string UserId { get; set; }

    private Dictionary<
        FacingDirection,
        Dictionary<CharacterPartCategory, Transform>
    > _cachedPartsPerDirections =
        new Dictionary<FacingDirection, Dictionary<CharacterPartCategory, Transform>>();
    private Dictionary<string, Texture2D> cachedCategoryTextures =
        new Dictionary<string, Texture2D>();

    private SpriteConfigBuilder builder;
    private SpriteConfigDirector director;

    private void Awake()
    {
        logger.Log("[SpriteLoader] Initializing sprites for character", this);

        builder = new SpriteConfigBuilder();
        director = new SpriteConfigDirector(builder);

        CachePartTransforms();

        // Notify that SpriteLoader is ready (used on hud controller to load weapon sprites)
        OnSpriteLoaderReady?.Invoke(this);

        if (spriteManager != null)
        {
            spriteManager.OnArmorHelmetChange += ChangeHelmet;
            spriteManager.OnArmorBodyChange += ChangeBody;
            spriteManager.OnArmorLegsChange += ChangeLegs;
            spriteManager.OnHairChange += ChangeHair;
            spriteManager.OnBeardChange += ChangeBeard;
            spriteManager.OnEyeBrowsChange += ChangeEyeBrows;
            spriteManager.OnEyesChange += ChangeEyes;
            spriteManager.OnMouthChange += ChangeMouth;
            spriteManager.OnBackChange += ChangeBack;
            spriteManager.OnEarringsChange += ChangeEarrings;
            spriteManager.OnMaskChange += ChangeMask;
        }
        _ = InitCharacterSpritesAsync();
    }

    private void OnDestroy()
    {
        if (spriteManager != null)
        {
            spriteManager.OnArmorHelmetChange -= ChangeHelmet;
            spriteManager.OnArmorBodyChange -= ChangeBody;
            spriteManager.OnArmorLegsChange -= ChangeLegs;
            spriteManager.OnHairChange -= ChangeHair;
            spriteManager.OnBeardChange -= ChangeBeard;
            spriteManager.OnEyeBrowsChange -= ChangeEyeBrows;
            spriteManager.OnEyesChange -= ChangeEyes;
            spriteManager.OnMouthChange -= ChangeMouth;
            spriteManager.OnBackChange -= ChangeBack;
            spriteManager.OnEarringsChange -= ChangeEarrings;
            spriteManager.OnMaskChange -= ChangeMask;
        }

        // Clean up cached textures to prevent memory leaks
        foreach (var texture in cachedCategoryTextures.Values)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }
        cachedCategoryTextures.Clear();
    }

    public void StartLoadingSprites()
    {
        logger.Log($"[SpriteLoader] StartLoadingSprites called with UserId: '{UserId}'", this);
        _ = InitCharacterSpritesAsync();
    }

    private void CachePartTransforms()
    {
        foreach (FacingDirection direction in Enum.GetValues(typeof(FacingDirection)))
        {
            var directionTransform = FindChildRecursive(transform, direction.ToString());
            if (directionTransform != null)
            {
                var cachedParts = new Dictionary<CharacterPartCategory, Transform>();
                cachedParts[CharacterPartCategory.Hair] = FindChildRecursive(
                    directionTransform,
                    "Hair"
                );
                cachedParts[CharacterPartCategory.Beard] = FindChildRecursive(
                    directionTransform,
                    "Beard"
                );
                cachedParts[CharacterPartCategory.EyeBrows] = FindChildRecursive(
                    directionTransform,
                    "Eyesbrows"
                );
                cachedParts[CharacterPartCategory.Eyes] = FindChildRecursive(
                    directionTransform,
                    "Eyes"
                );
                cachedParts[CharacterPartCategory.Mouth] = FindChildRecursive(
                    directionTransform,
                    "Mouth"
                );

                cachedParts[CharacterPartCategory.ArmorBody] = directionTransform
                    .Find("UpperBody")
                    ?.Find("Armor");
                cachedParts[CharacterPartCategory.ArmorHelmet] = FindChildRecursive(
                    directionTransform,
                    "Helmet"
                );

                cachedParts[CharacterPartCategory.ArmorArmR] = FindChildRecursive(
                    directionTransform,
                    "ArmR"
                )
                    ?.Find("Armor");
                cachedParts[CharacterPartCategory.ArmorArmL] = FindChildRecursive(
                    directionTransform,
                    "ArmL"
                )
                    ?.Find("Armor");
                cachedParts[CharacterPartCategory.ArmorSleeveR] = FindChildRecursive(
                    directionTransform,
                    "ArmR"
                )
                    ?.Find("Sleeve");
                cachedParts[CharacterPartCategory.ArmorSleeveL] = FindChildRecursive(
                    directionTransform,
                    "ArmL"
                )
                    ?.Find("Sleeve");
                cachedParts[CharacterPartCategory.ArmorHandR] = FindChildRecursive(
                    directionTransform,
                    "HandR"
                )
                    ?.Find("Armor");
                cachedParts[CharacterPartCategory.ArmorHandL] = FindChildRecursive(
                    directionTransform,
                    "HandL"
                )
                    ?.Find("Armor");
                cachedParts[CharacterPartCategory.ArmorLegR] = FindChildRecursive(
                    directionTransform,
                    "LegR"
                )
                    ?.Find("Armor");
                cachedParts[CharacterPartCategory.ArmorLegL] = FindChildRecursive(
                    directionTransform,
                    "LegL"
                )
                    ?.Find("Armor");

                cachedParts[CharacterPartCategory.EarringR] = FindChildRecursive(
                    directionTransform,
                    "EarringR"
                );
                cachedParts[CharacterPartCategory.EarringL] = FindChildRecursive(
                    directionTransform,
                    "EarringL"
                );
                cachedParts[CharacterPartCategory.Back] = FindChildRecursive(
                    directionTransform,
                    "Back"
                );
                cachedParts[CharacterPartCategory.Mask] = FindChildRecursive(
                    directionTransform,
                    "Mask"
                );
                cachedParts[CharacterPartCategory.WeaponR] = FindChildRecursive(
                    directionTransform,
                    "PrimaryWeapon"
                );

                _cachedPartsPerDirections[direction] = cachedParts;
            }
            else
            {
                logger.Log(
                    $"Direction transform not found: {direction} under {gameObject.name}",
                    this,
                    Logging.LogType.Warning
                );
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        Transform result = parent.Find(childName);
        if (result != null)
            return result;

        foreach (Transform child in parent)
        {
            result = FindChildRecursive(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }

    /// <summary>
    /// Replaces the sprite of a part at the given path.
    /// Example: ReplacePartSprite(newSprite, "Parent", "Child", "TargetObject")
    /// </summary>
    private void ReplacePartSprite(
        Sprite newSprite,
        FacingDirection direction,
        params CharacterPartCategory[] pathSegments
    )
    {
        if (pathSegments == null || pathSegments.Length == 0)
        {
            logger.Log(
                "No path segments provided to ReplacePartSprite",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        if (
            !_cachedPartsPerDirections.TryGetValue(
                direction,
                out Dictionary<CharacterPartCategory, Transform> partsDict
            )
        )
        {
            logger.Log(
                $"Direction not cached: {direction} under {gameObject.name}",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        if (
            !partsDict.TryGetValue(pathSegments[0], out Transform currentTransform)
            || currentTransform == null
        )
        {
            logger.Log(
                $"Root child not found: {pathSegments[0]} under {gameObject.name}",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        SpriteRenderer spriteRenderer = currentTransform.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            logger.Log(
                $"SpriteRenderer not found at path: {string.Join("/", pathSegments)}",
                this,
                Logging.LogType.Warning
            );
        }
        spriteRenderer.sprite = newSprite;
        spriteRenderer.enabled = (newSprite != null);
    }

    /* --- PART CHANGE HANDLERS --- */

    private void ChangeHelmet(Texture2D texture)
    {
        ChangeTexture(texture, director.BuildArmorHelmetSpriteConfig());
        logger.Log("[SpriteLoader] Changed Armor Helmet sprites", this);
    }

    private void ChangeBody(Texture2D texture)
    {
        ChangeTexture(texture, director.BuildArmorBodySpriteConfig());
        logger.Log("[SpriteLoader] Changed Armor Body sprites", this);
    }

    private void ChangeLegs(Texture2D texture)
    {
        ChangeTexture(texture, director.BuildArmorLegsSpriteConfig());
        logger.Log("[SpriteLoader] Changed Armor Legs sprites", this);
    }

    private void ChangeHair(Texture2D texture)
    {
        ChangeTexture(texture, director.BuildHairSpriteConfig());
        logger.Log("[SpriteLoader] Changed Hair sprites", this);
    }

    private void ChangeBeard(Texture2D texture)
    {
        ChangeTexture(texture, director.BuildBeardSpriteConfig());
        logger.Log("[SpriteLoader] Changed Beard sprites", this);
    }

    private void ChangeEyeBrows(Texture2D texture)
    {
        ChangeTexture(texture, director.BuildEyeBrowsSpriteConfig());
        logger.Log("[SpriteLoader] Changed EyeBrows sprites", this);
    }

    private void ChangeEyes(Texture2D texture)
    {
        ChangeTexture(texture, director.BuildEyesSpriteConfig());
        logger.Log("[SpriteLoader] Changed Eyes sprites", this);
    }

    private void ChangeMouth(Texture2D texture)
    {
        ChangeTexture(texture, director.BuildMouthSpriteConfig());
        logger.Log("[SpriteLoader] Changed Mouth sprites", this);
    }

    private void ChangeBack(Texture2D texture)
    {
        ChangeTexture(texture, director.BuildBackSpriteConfig());
        logger.Log("[SpriteLoader] Changed Back sprites", this);
    }

    private void ChangeEarrings(Texture2D texture)
    {
        ChangeTexture(texture, director.BuildEarringsSpriteConfig());
        logger.Log("[SpriteLoader] Changed Earrings sprites", this);
    }

    private void ChangeMask(Texture2D texture)
    {
        ChangeTexture(texture, director.BuildMaskSpriteConfig());
        logger.Log("[SpriteLoader] Changed Mask sprites", this);
    }

    private void ChangeWeapon(Texture2D texture)
    {
        ChangeTexture(texture, director.BuildWeaponSpriteConfig(), true);
        logger?.Log("[SpriteLoader] Changed Weapon sprites", this);
    }

    /// <summary>
    /// Changes the texture for multiple sprite configurations [or removes it if texture is null!].
    /// </summary>
    private void ChangeTexture(
        Texture2D texture,
        List<SpriteConfig> confs,
        bool useFullRectIfZero = false
    )
    {
        foreach (var config in confs)
        {
            Sprite sprite = null;

            if (texture != null)
            {
                Rect finalRect = config.Rect;
                if (useFullRectIfZero && (finalRect.width == 0 || finalRect.height == 0))
                {
                    finalRect = new Rect(0, 0, texture.width, texture.height);
                }

                sprite = Sprite.Create(texture, finalRect, config.Pivot, config.PixelsPerUnit);
            }

            ReplacePartSprite(sprite, config.Direction, config.Part);
        }
    }

    /* --- INITIALIZATION UTILS --- */

    private async Task InitCharacterSpritesAsync()
    {
        // Fetch categories
        var categoriesResponse = await assetsService.GetCategoriesAsync();
        if (categoriesResponse == null)
        {
            logger.Log("Failed to fetch categories", this, Logging.LogType.Warning);
            return;
        }

        // Fetch character info
        logger.Log($"[SpriteLoader] Fetching character info for UserId: '{UserId}'", this);
        var characterInfo = await playerService.GetCharacterInfoAsync(UserId);
        if (characterInfo == null)
        {
            logger.Log("Failed to fetch character info", this, Logging.LogType.Warning);
            return;
        }

        // All data fetched, apply sprites
        if (categoriesResponse.category_list == null)
        {
            logger.Log("No categories found in response.", this, Logging.LogType.Warning);
            return;
        }

        Dictionary<string, string> existingCategories = new Dictionary<string, string>();
        foreach (var category in categoriesResponse.category_list)
        {
            existingCategories[category.category_id] = category.category_name;
        }

        if (characterInfo.category_sprites == null)
        {
            logger.Log("No character sprites found in response.", this, Logging.LogType.Warning);
            return;
        }

        foreach (var entry in characterInfo.category_sprites)
        {
            if (!cachedCategoryTextures.TryGetValue(entry.Value, out Texture2D texture))
            {
                texture = await assetsService.DownloadTexture2D(entry.Value);
                if (texture == null)
                    continue;
                cachedCategoryTextures[entry.Value] = texture;
            }

            string categoryName = existingCategories.TryGetValue(entry.Key, out var name)
                ? name
                : "";
            CharacterPartCategory category = spriteManager.GetPartCategoryFromCategoryName(
                categoryName
            );
            if (category != CharacterPartCategory.None)
            {
                switch (category)
                {
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
                    case CharacterPartCategory.Hair:
                        ChangeHair(texture);
                        break;
                    case CharacterPartCategory.Beard:
                        ChangeBeard(texture);
                        break;
                    case CharacterPartCategory.EyeBrows:
                        ChangeEyeBrows(texture);
                        break;
                    case CharacterPartCategory.Eyes:
                        ChangeEyes(texture);
                        break;
                    case CharacterPartCategory.Back:
                        ChangeBack(texture);
                        break;
                    case CharacterPartCategory.EarringR:
                    case CharacterPartCategory.EarringL:
                        ChangeEarrings(texture);
                        break;
                    case CharacterPartCategory.Mask:
                        ChangeMask(texture);
                        break;
                    default:
                        logger.Log(
                            $"No handler for category: {category} under {gameObject.name}",
                            this,
                            Logging.LogType.Warning
                        );
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Method to equip a weapon by spriteId.
    /// Downloads (or loads from cache) the weapon sprite from the API and applies it to the character.
    /// </summary>
    public void EquipWeapon(string worldId, string spriteId)
    {
        if (string.IsNullOrEmpty(spriteId))
        {
            logger?.Log(
                "EquipWeapon called with null or empty spriteId",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        if (itemAssetsService == null)
        {
            logger?.Log("ItemAssetsService is not assigned!", this, Logging.LogType.Error);
            return;
        }

        try
        {
            var coroutine = StartCoroutine(EquipWeaponCoroutine(spriteId));
            //Debug.Log($"[SpriteLoader DEBUG] StartCoroutine returned: {(coroutine != null ? "SUCCESS" : "NULL")}");
        }
        catch (System.Exception ex)
        {
            logger?.Log(
                $"EquipWeaponCoroutine threw exception: {ex.Message}\n{ex.StackTrace}",
                this,
                Logging.LogType.Error
            );
        }
    }

    private System.Collections.IEnumerator EquipWeaponCoroutine(string spriteId)
    {
        var downloadTask = itemAssetsService.DownloadItemSpriteAsync(spriteId);

        while (!downloadTask.IsCompleted)
        {
            yield return null;
        }

        if (downloadTask.IsFaulted)
        {
            logger?.Log(
                $"Download task faulted: {downloadTask.Exception}",
                this,
                Logging.LogType.Error
            );
            yield break;
        }

        Texture2D weaponTexture = downloadTask.Result;

        if (weaponTexture == null)
        {
            logger?.Log(
                $"Failed to download weapon sprite: {spriteId}",
                this,
                Logging.LogType.Error
            );
            yield break;
        }

        ChangeWeapon(weaponTexture);
        logger?.Log($"Successfully equipped weapon: {spriteId}", this);
    }

    /// <summary>
    /// Method to unequip the current weapon (removes weapon sprites).
    /// </summary>
    public void UnequipWeapon()
    {
        logger?.Log("Unequipping weapon", this);
        ChangeWeapon(null);
    }
}
