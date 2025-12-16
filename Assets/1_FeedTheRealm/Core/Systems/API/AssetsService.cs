using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace API {
    /// <summary>
    /// Service to manage assets downloading.
    /// </summary>
    [CreateAssetMenu(fileName = "AssetsService", menuName = "Scriptable Objects/API/AssetsService")]
    public class AssetsService : ScriptableObject {
        [Header("Server settings")]
        [SerializeField]
        public string Hostname;

        [SerializeField]
        public int Port;

        [Header("Session settings")]
        [SerializeField]
        private Session.Session session;

        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        /// <summary>
        /// Retrieve the list of categories for sprites.
        /// </summary>
        public IEnumerator GetCategories(System.Action<SpriteCategoryListResponse, string> handler) {
            var url = $"http://{Hostname}:{Port}/assets/sprites/categories";
            var uwr = new UnityWebRequest(url, "GET");
            uwr.downloadHandler = new DownloadHandlerBuffer();

            uwr.SetRequestHeader("Authorization", $"Bearer {session.APIToken}");

            yield return uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError) {
                var res = string.IsNullOrEmpty(responseText) ? null : JsonUtility.FromJson<ErrorResponse>(responseText);
                logger.Log($"GetCategories error: {(res != null ? $"{res.title}: {res.detail}" : responseText)} - {responseText}", this, Logging.LogType.Error);
                handler?.Invoke(null, res.detail);
            } else {
                var res = JsonUtility.FromJson<DataEnvelope<SpriteCategoryListResponse>>(responseText);
                handler?.Invoke(res.data, "");
            }
        }

        /// <summary>
        /// Retrieve the list of sprites for a given category.
        /// </summary>
        public IEnumerator GetSpritesByCategory(string categoryId, System.Action<SpritesListResponse, string> handler) {
            var url = $"http://{Hostname}:{Port}/assets/sprites/categories/{categoryId}";
            var uwr = new UnityWebRequest(url, "GET");
            uwr.downloadHandler = new DownloadHandlerBuffer();

            uwr.SetRequestHeader("Authorization", $"Bearer {session.APIToken}");

            yield return uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError) {
                var res = string.IsNullOrEmpty(responseText) ? null : JsonUtility.FromJson<ErrorResponse>(responseText);
                logger.Log($"GetSpritesByCategory error: {(res != null ? $"{res.title}: {res.detail}" : responseText)} - {responseText}", this, Logging.LogType.Error);
                handler?.Invoke(null, res.detail);
            } else {
                var res = JsonUtility.FromJson<DataEnvelope<SpritesListResponse>>(responseText);
                handler?.Invoke(res.data, "");
            }
        }

        /// <summary>
        /// Download the sprite with the given id.
        /// </summary>
        public IEnumerator DownloadTexture2D(string spriteId, System.Action<Texture2D> handler) {
            var url = $"http://{Hostname}:{Port}/assets/sprites/{spriteId}";

            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url)) {
                uwr.SetRequestHeader("Authorization", $"Bearer {session.APIToken}");
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError) {
                    var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;
                    var res = string.IsNullOrEmpty(responseText) ? null : JsonUtility.FromJson<ErrorResponse>(responseText);
                    logger.Log($"DownloadSprite error: {(res != null ? $"{res.title}: {res.detail}" : responseText)}", this, Logging.LogType.Error);
                    handler?.Invoke(null);
                } else {
                    Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;

                    logger.Log($"DownloadSprite success for sprite_id: {spriteId}", this);
                    handler?.Invoke(texture);
                }
            }
        }
    }
}
