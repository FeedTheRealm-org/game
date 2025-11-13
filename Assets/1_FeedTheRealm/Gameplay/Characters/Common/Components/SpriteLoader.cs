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
            spriteManager.onHeadChange += changeHead;
            spriteManager.onHairChange += changeHair;
            spriteManager.onFaceHairChange += changeFaceHair;
            spriteManager.onEyeBackChange += changeEyeBack;
            spriteManager.onClothChange += changeCloth;
            spriteManager.onArmorChange += changeArmor;
            spriteManager.onHelmetChange += changeHelmet;
            spriteManager.onFootBootChange += changeFootBoot;
            spriteManager.onFootClothChange += changeFootCloth;
            spriteManager.onCapeChange += changeCape;
        }
    }

    private void OnDestroy() {
        if (spriteManager != null) {
            spriteManager.onHeadChange -= changeHead;
            spriteManager.onHairChange -= changeHair;
            spriteManager.onFaceHairChange -= changeFaceHair;
            spriteManager.onEyeBackChange -= changeEyeBack;
            spriteManager.onClothChange -= changeCloth;
            spriteManager.onArmorChange -= changeArmor;
            spriteManager.onHelmetChange -= changeHelmet;
            spriteManager.onFootBootChange -= changeFootBoot;
            spriteManager.onFootClothChange -= changeFootCloth;
            spriteManager.onCapeChange -= changeCape;
        }
    }

    /// <summary>
    /// Replaces the sprite of a given part.
    /// </summary>
    private void replacePartSprite(string partName, Sprite newSprite) {
        GameObject partObject = GameObject.Find(partName);
        if (partObject != null) {
            SpriteRenderer spriteRenderer = partObject.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) {
                spriteRenderer.sprite = newSprite;
            } else {
                logger.Log($"SpriteRenderer not found on part: {partName}", this, Logging.LogType.Warning);
            }
        } else {
            logger.Log($"Part GameObject not found: {partName}", this, Logging.LogType.Warning);
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
            Sprite newSprite = Sprite.Create(
                texture,
                rect,
                pivot,
                pxPerUnit
            );
            callback(newSprite);
        }));
    }

    /* --- PART CHANGE HANDLERS --- */

    private void changeHead(string spriteId) {
        downloadSprite(spriteId, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32f, (newHead) => {
            replacePartSprite("P_Head", newHead);
        });
    }

    private void changeHair(string spriteId) {
        downloadSprite(spriteId, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32f, (newHair) => {
            replacePartSprite("P_Hair", newHair);
        });
    }

    private void changeFaceHair(string spriteId) {
        downloadSprite(spriteId, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32f, (newFaceHair) => {
            replacePartSprite("P_FaceHair", newFaceHair);
        });
    }

    private void changeEyeBack(string spriteId) {
        downloadSprite(spriteId, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32f, (newEyeBack) => {
            replacePartSprite("P_EyeBack", newEyeBack);
        });
    }

    private void changeCloth(string spriteId) {
        downloadSprite(spriteId, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32f, (newCloth) => {
            replacePartSprite("P_Cloth", newCloth);
        });
    }

    private void changeArmor(string spriteId) {
        downloadSprite(spriteId, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32f, (newArmor) => {
            replacePartSprite("P_Armor", newArmor);
        });
    }

    private void changeHelmet(string spriteId) {
        downloadSprite(spriteId, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32f, (newHelmet) => {
            replacePartSprite("P_Helmet", newHelmet);
        });
    }

    private void changeFootBoot(string spriteId) {
        downloadSprite(spriteId, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32f, (newFootBoot) => {
            replacePartSprite("P_FootBoot", newFootBoot);
        });
    }

    private void changeFootCloth(string spriteId) {
        downloadSprite(spriteId, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32f, (newFootCloth) => {
            replacePartSprite("P_FootCloth", newFootCloth);
        });
    }

    private void changeCape(string spriteId) {
        downloadSprite(spriteId, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32f, (newCape) => {
            replacePartSprite("P_Cape", newCape);
        });
    }
}
