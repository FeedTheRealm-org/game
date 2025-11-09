using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class SpriteLoader : MonoBehaviour {
    public void ChangeHair(string textureName) {
        StartCoroutine(LoadTextureFromServer(textureName));
    }

    IEnumerator LoadTextureFromServer(string textureName) {
        string url = "http://localhost:8080/" + textureName;

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url)) {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success) {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);

                Sprite newHair = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );

                GameObject.Find("P_Hair").GetComponentInChildren<SpriteRenderer>().sprite = newHair;
            } else {
                Debug.LogError("Error loading texture: " + uwr.error);
            }
        }
    }
}
