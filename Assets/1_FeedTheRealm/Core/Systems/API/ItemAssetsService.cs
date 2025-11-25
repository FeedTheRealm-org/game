using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace API {
    /// <summary>
    /// Service to download item sprites from API.
    /// Handles sprite downloads for items system.
    /// Route: /assets/sprites/items/{spriteId}?category={category}
    /// Separated from AssetsService (character editor sprites).
    /// </summary>
    [CreateAssetMenu(fileName = "ItemAssetsService", menuName = "Scriptable Objects/API/ItemAssetsService")]
    public class ItemAssetsService : ScriptableObject {
        [Header("Server settings")]
        [SerializeField]
        public string Hostname;

        [SerializeField]
        public int Port;

        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        /// <summary>
        /// Download sprite by spriteId (without category validation).
        /// Full URL: /assets/sprites/items/{spriteId}
        /// </summary>
        public IEnumerator DownloadItemSprite(string spriteId, System.Action<Texture2D> handler) {
            var url = $"http://{Hostname}:{Port}/assets/sprites/items/{spriteId}";
            var uwr = UnityWebRequestTexture.GetTexture(url);

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError ||
                uwr.result == UnityWebRequest.Result.ProtocolError) {
                logger?.Log($"DownloadItemSprite error for {spriteId}: {uwr.error}",
                          this, Logging.LogType.Error);
                handler?.Invoke(null);
            } else {
                var texture = DownloadHandlerTexture.GetContent(uwr);
                logger?.Log($"DownloadItemSprite success: {spriteId}", this);
                handler?.Invoke(texture);
            }
        }

        /// <summary>
        /// Download sprite by spriteId with category validation.
        /// Uses query parameter for category validation.
        /// Full URL: /assets/sprites/items/{spriteId}?category={category}
        /// </summary>
        public IEnumerator DownloadItemSpriteByCategory(string spriteId, string category, System.Action<Texture2D> handler) {
            var url = $"http://{Hostname}:{Port}/assets/sprites/items/{spriteId}?category={category}";
            var uwr = UnityWebRequestTexture.GetTexture(url);

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError ||
                uwr.result == UnityWebRequest.Result.ProtocolError) {
                logger?.Log($"DownloadItemSpriteByCategory error for {spriteId} (category: {category}): {uwr.error}",
                          this, Logging.LogType.Error);
                handler?.Invoke(null);
            } else {
                var texture = DownloadHandlerTexture.GetContent(uwr);
                logger?.Log($"DownloadItemSpriteByCategory success: {spriteId} (category: {category})", this);
                handler?.Invoke(texture);
            }
        }

    }
}
