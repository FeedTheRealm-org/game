using UnityEngine;

public class SpriteLoader : MonoBehaviour {
    [SerializeField]
    private SpriteManager spriteManager;

    [Header("Services settings")]
    [SerializeField]
    private API.AssetsService assetsService;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    private void Awake() {
        if (spriteManager != null) {
            spriteManager.onHairChange += changeHair;
            spriteManager.onFaceHairChange += changeFaceHair;
            spriteManager.onEyeBackChange += changeEyeBack;
            spriteManager.onClothChange += changeCloth;
            spriteManager.onArmorChange += changeArmor;
            spriteManager.onHelmetChange += changeHelmet;
            spriteManager.onFootBootChange += changeFootBoot;
            spriteManager.onCapeChange += changeCape;
        }
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

        GameObject currentObject = GameObject.Find(pathSegments[0]);
        if (currentObject == null) {
            logger.Log($"Root GameObject not found: {pathSegments[0]}", this, Logging.LogType.Warning);
            return;
        }

        for (int i = 1; i < pathSegments.Length; i++) {
            Transform childTransform = currentObject.transform.Find(pathSegments[i]);
            if (childTransform == null) {
                logger.Log($"Child GameObject not found: {pathSegments[i]} under {currentObject.name}", this, Logging.LogType.Warning);
                return;
            }
            currentObject = childTransform.gameObject;
        }

        SpriteRenderer spriteRenderer = currentObject.GetComponentInChildren<SpriteRenderer>();
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
            if (rect.width == 0f) {
                rect.width = texture.width;
            }
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
        downloadSpriteFullSize(spriteId, new Vector2(0.5f, 0.5f), 32f, (newHair) => {
            replacePartSprite(newHair, "P_Hair");
        });
    }

    private void changeFaceHair(string spriteId) {
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
        downloadSpriteCustom(spriteId, 10f, 12f, 12f, 9f, new Vector2(0.5f, 0.5f), 32f, (newArmor) => {
            replacePartSprite(newArmor, "P_ArmorBody");
        });
    }

    private void changeHelmet(string spriteId) {
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
        downloadSpriteFullSize(spriteId, new Vector2(0.47826087f, 0.90909094f), 32f, (newCape) => {
            replacePartSprite(newCape, "P_Back");
        });
    }
}
