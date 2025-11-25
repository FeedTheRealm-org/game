using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace API {
    /// <summary>
    /// Service to download item sprites from API.
    /// Handles sprite downloads for items system.
    /// Routes: /assets/sprites/items/by-id/{spriteId} or /assets/sprites/items/{category}/{spriteId}
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
        /// Download sprite by spriteId directly (without category).
        /// Full URL will be: /assets/sprites/items/by-id/{spriteId}
        /// </summary>
        public IEnumerator DownloadItemSprite(string spriteId, System.Action<Texture2D> handler) {
            var url = $"http://{Hostname}:{Port}/assets/sprites/items/by-id/{spriteId}";
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
        /// Download sprite by category and spriteId separately.
        /// Recommended method - matches backend structure.
        /// Example: category="weapon", spriteId="uuid-here"
        /// Full URL: /assets/sprites/items/{category}/{spriteId}
        /// </summary>
        public IEnumerator DownloadItemSpriteByCategory(string category, string spriteId, System.Action<Texture2D> handler) {
            var url = $"http://{Hostname}:{Port}/assets/sprites/items/{category}/{spriteId}";
            var uwr = UnityWebRequestTexture.GetTexture(url);

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError || 
                uwr.result == UnityWebRequest.Result.ProtocolError) {
                logger?.Log($"DownloadItemSpriteByCategory error for {category}/{spriteId}: {uwr.error}", 
                          this, Logging.LogType.Error);
                handler?.Invoke(null);
            } else {
                var texture = DownloadHandlerTexture.GetContent(uwr);
                logger?.Log($"DownloadItemSpriteByCategory success: {category}/{spriteId}", this);
                handler?.Invoke(texture);
            }
        }

    }
}
