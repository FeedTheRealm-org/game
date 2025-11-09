using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class SpriteLoader : MonoBehaviour {
    [SerializeField]
    private SpriteManager spriteManager;

    private void Awake() {
        if (spriteManager != null) {
            spriteManager.onHairChange += changeHair;
        }
    }

    private void changeHair(string textureName) {
        StartCoroutine(loadTextureFromServer(textureName));
    }

    private IEnumerator loadTextureFromServer(string textureName) {
        string url = "http://localhost:8000/assets/sprites/" + textureName;

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url)) {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success) {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);

                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                Sprite newHair = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    32f
                );

                GameObject.Find("P_Hair").GetComponentInChildren<SpriteRenderer>().sprite = newHair;
            } else {
                Debug.LogError("Error loading texture: " + uwr.error);
            }
        }
    }
}
