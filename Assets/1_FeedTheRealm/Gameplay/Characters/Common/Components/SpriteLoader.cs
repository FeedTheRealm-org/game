using UnityEngine;

public class SpriteLoader : MonoBehaviour {
    [SerializeField]
    private SpriteManager spriteManager;

    [Header("Services settings")]
    [SerializeField]
    private API.AssetsService assetsService;

    private void Awake() {
        if (spriteManager != null) {
            spriteManager.onHairChange += changeHair;
        }
    }

    private void changeHair(string spriteId) {
        StartCoroutine(assetsService.DownloadSprite(spriteId, (texture) => {
            if (texture == null) {
                return;
            }
            Sprite newHair = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                32f
            );
            GameObject.Find("P_Hair").GetComponentInChildren<SpriteRenderer>().sprite = newHair;
        }));
    }
}
