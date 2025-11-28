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

    private Dictionary<string, Transform> cachedParts = new Dictionary<string, Transform>();

    // Removed SpriteManager event subscriptions to prevent cross-player sprite changes in multiplayer

    private void CachePartTransforms() {
        cachedParts["P_Hair"] = FindChildRecursive(transform, "P_Hair");
        cachedParts["P_Mustache"] = FindChildRecursive(transform, "P_Mustache");
        cachedParts["P_REye"] = FindChildRecursive(transform, "P_REye");
        cachedParts["P_LEye"] = FindChildRecursive(transform, "P_LEye");
        cachedParts["P_ClothBody"] = FindChildRecursive(transform, "P_ClothBody");
        cachedParts["P_ArmorBody"] = FindChildRecursive(transform, "P_ArmorBody");
        cachedParts["P_Helmet"] = FindChildRecursive(transform, "P_Helmet");
        cachedParts["P_RCloth"] = FindChildRecursive(transform, "P_RCloth");
        cachedParts["P_LCloth"] = FindChildRecursive(transform, "P_LCloth");
        cachedParts["P_Back"] = FindChildRecursive(transform, "P_Back");
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
        CachePartTransforms();
        StartCoroutine(initCharacterSpritesCoroutine());
    }

    private void OnDestroy() {
        if (spriteManager != null) {
            spriteManager.onHairChange -= changeHair;
            spriteManager.onFaceHairChange -= changeFaceHair;
            spriteManager.onEyeBackChange -= changeEyeBack;
            spriteManager.onClothChange -= changeCloth;
            spriteManager.onArmorChange -= changeArmor;
            spriteManager.onHelmetChange -= changeHelmet;
            spriteManager.onFootBootChange -= changeFootBoot;
            spriteManager.onCapeChange -= changeCape;
        }
    }

    /// <summary>
    /// Replaces the sprite of a part at the given path.
    /// Example: replacePartSprite(newSprite, "Parent", "Child", "TargetObject")
    /// </summary>
    private void replacePartSprite(Sprite newSprite, params string[] pathSegments) {
        if (pathSegments == null || pathSegments.Length == 0) {
            logger.Log("No path segments provided to replacePartSprite", this, Logging.LogType.Warning);
            return;
        }

        if (!cachedParts.TryGetValue(pathSegments[0], out Transform currentTransform) || currentTransform == null) {
            logger.Log($"Root child not found: {pathSegments[0]} under {gameObject.name}", this, Logging.LogType.Warning);
            return;
        }

        for (int i = 1; i < pathSegments.Length; i++) {
            currentTransform = currentTransform.Find(pathSegments[i]);
            if (currentTransform == null) {
                logger.Log($"Child GameObject not found: {pathSegments[i]} in path {string.Join("/", pathSegments)}", this, Logging.LogType.Warning);
                return;
            }
        }

        SpriteRenderer spriteRenderer = currentTransform.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) {
            spriteRenderer.sprite = newSprite;
        } else {
            logger.Log($"SpriteRenderer not found at path: {string.Join("/", pathSegments)}", this, Logging.LogType.Warning);
        }
    }

    /// <summary>
    /// Downloads a sprite from the AssetsService and creates a sprite.
    /// </summary>
    private void downloadSprite(string spriteId, Rect rect, Vector2 pivot, float pxPerUnit, System.Action<Sprite> callback) {
        StartCoroutine(assetsService.DownloadSprite(spriteId, (texture) => {
            if (texture == null) {
                logger.Log($"Failed to download sprite with ID: {spriteId}", this, Logging.LogType.Error);
                return;
            }
            logger.Log($"Successfully downloaded sprite with ID: {spriteId}", this, Logging.LogType.Info);
            rect.width = rect.width > 0f ? rect.width : texture.width;
            rect.height = rect.height > 0f ? rect.height : texture.height;

            // Clamp rect to texture bounds in case of invalid values
            rect.x = Mathf.Max(0, rect.x);
            rect.y = Mathf.Max(0, rect.y);
            rect.width = Mathf.Min(rect.width, texture.width - rect.x);
            rect.height = Mathf.Min(rect.height, texture.height - rect.y);

            Sprite newSprite = Sprite.Create(
                texture,
                rect,
                pivot,
                pxPerUnit
            );
            callback(newSprite);
        }));
    }

    /// <summary>
    /// Convenience method for downloading sprites with auto-sized rects.
    /// </summary>
    private void downloadSpriteFullSize(string spriteId, Vector2 pivot, float pxPerUnit, System.Action<Sprite> callback) {
        downloadSprite(spriteId, new Rect(0f, 0f, 0f, 0f), pivot, pxPerUnit, callback);
    }

    /// <summary>
    /// Convenience method for downloading sprites with custom rect.
    /// </summary>
    private void downloadSpriteCustom(string spriteId, float x, float y, float width, float height, Vector2 pivot, float pxPerUnit, System.Action<Sprite> callback) {
        downloadSprite(spriteId, new Rect(x, y, width, height), pivot, pxPerUnit, callback);
    }

    /* --- PART CHANGE HANDLERS --- */

    private void changeHair(string spriteId) {
        if (string.IsNullOrEmpty(spriteId)) {
            replacePartSprite(null, "P_Hair"); // Remove sprite
            return;
        }
        downloadSpriteFullSize(spriteId, new Vector2(0.5f, 0.5f), 32f, (newHair) => {
            replacePartSprite(newHair, "P_Hair");
        });
    }

    private void changeFaceHair(string spriteId) {
        if (string.IsNullOrEmpty(spriteId)) {
            replacePartSprite(null, "P_Mustache"); // Remove sprite
            return;
        }
        downloadSpriteFullSize(spriteId, new Vector2(0.5f, 0.5f), 32f, (newFaceHair) => {
            replacePartSprite(newFaceHair, "P_Mustache");
        });
    }

    private void changeEyeBack(string spriteId) {
        downloadSpriteFullSize(spriteId, new Vector2(0.5f, 0.5f), 32f, (newEyeBack) => {
            replacePartSprite(newEyeBack, "P_REye", "PivotBack");
            replacePartSprite(newEyeBack, "P_LEye", "PivotBack");
        });
    }

    private void changeCloth(string spriteId) {
        downloadSpriteCustom(spriteId, 10f, 11f, 12f, 10f, new Vector2(0.5f, 0.5f), 32f, (newCloth) => {
            replacePartSprite(newCloth, "P_ClothBody");
        });
    }

    private void changeArmor(string spriteId) {
        if (string.IsNullOrEmpty(spriteId)) {
            replacePartSprite(null, "P_ArmorBody"); // Remove sprite
            return;
        }
        downloadSpriteCustom(spriteId, 10f, 12f, 12f, 9f, new Vector2(0.5f, 0.5f), 32f, (newArmor) => {
            replacePartSprite(newArmor, "P_ArmorBody");
        });
    }

    private void changeHelmet(string spriteId) {
        if (string.IsNullOrEmpty(spriteId)) {
            replacePartSprite(null, "P_Helmet"); // Remove sprite
            return;
        }
        downloadSpriteFullSize(spriteId, new Vector2(0.5f, 0.5f), 32f, (newHelmet) => {
            replacePartSprite(newHelmet, "P_Helmet");
        });
    }

    private void changeFootBoot(string spriteId) {
        downloadSpriteCustom(spriteId, 6f, 0f, 4f, 7f, new Vector2(0.5f, 0.5f), 32f, (newFootBoot) => {
            replacePartSprite(newFootBoot, "P_RCloth");
            replacePartSprite(newFootBoot, "P_LCloth");
        });
    }

    private void changeCape(string spriteId) {
        if (string.IsNullOrEmpty(spriteId)) {
            replacePartSprite(null, "P_Back"); // Remove sprite
            return;
        }
        downloadSpriteFullSize(spriteId, new Vector2(0.47826087f, 0.90909094f), 32f, (newCape) => {
            replacePartSprite(newCape, "P_Back");
        });
    }

    /* --- INITIALIZATION UTILS --- */
    private IEnumerator initCharacterSpritesCoroutine() {
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
            SpritePart category = spriteManager.GetSpritePartFromCategoryName(categoryName);
            if (category != SpritePart.None) {
                // Directly call the change method instead of using SpriteManager events
                switch (category) {
                    case SpritePart.Hair:
                        changeHair(entry.Value);
                        break;
                    case SpritePart.FaceHair:
                        changeFaceHair(entry.Value);
                        break;
                    case SpritePart.EyeBack:
                        changeEyeBack(entry.Value);
                        break;
                    case SpritePart.Cloth:
                        changeCloth(entry.Value);
                        break;
                    case SpritePart.Armor:
                        changeArmor(entry.Value);
                        break;
                    case SpritePart.Helmet:
                        changeHelmet(entry.Value);
                        break;
                    case SpritePart.FootBoot:
                        changeFootBoot(entry.Value);
                        break;
                    case SpritePart.Cape:
                        changeCape(entry.Value);
                        break;
                }
            }
        }
    }
}
