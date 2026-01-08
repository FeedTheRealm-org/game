using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace API
{
    /// <summary>
    /// Service to download item sprites from API.
    /// Handles sprite downloads for items system.
    /// Route: /assets/sprites/items/{spriteId}
    /// Separated from AssetsService (character editor sprites).
    /// </summary>
    [CreateAssetMenu(
        fileName = "ItemAssetsService",
        menuName = "Scriptable Objects/API/ItemAssetsService"
    )]
    public class ItemAssetsService : ScriptableObject
    {
        [Header("Server settings")]
        [SerializeField]
        public string Hostname;

        [SerializeField]
        public int Port;

        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        // Simple in-memory cache to avoid downloading the same sprite multiple times
        private readonly System.Collections.Generic.Dictionary<string, Texture2D> spriteCache =
            new System.Collections.Generic.Dictionary<string, Texture2D>();

        /// <summary>
        /// Download sprite by spriteId.
        /// Full URL: /assets/sprites/items/{spriteId}
        /// </summary>
        public async System.Threading.Tasks.Task<Texture2D> DownloadItemSpriteAsync(string spriteId)
        {
            if (string.IsNullOrEmpty(spriteId))
            {
                logger?.Log(
                    "DownloadItemSpriteAsync called with null or empty spriteId",
                    this,
                    Logging.LogType.Warning
                );
                return null;
            }

            // If already cached, return immediately without doing a web request
            if (spriteCache.TryGetValue(spriteId, out var cachedTexture))
            {
                logger?.Log($"DownloadItemSpriteAsync cache hit: {spriteId}", this);
                return cachedTexture;
            }

            var url = $"http://{Hostname}:{Port}/assets/sprites/items/{spriteId}";
            using var uwr = UnityWebRequestTexture.GetTexture(url);
            var asyncOp = uwr.SendWebRequest();
            await asyncOp;

            if (
                uwr.result == UnityWebRequest.Result.ConnectionError
                || uwr.result == UnityWebRequest.Result.ProtocolError
            )
            {
                logger?.Log(
                    $"DownloadItemSpriteAsync error for {spriteId}: {uwr.error}",
                    this,
                    Logging.LogType.Error
                );
                return null;
            }
            else
            {
                var texture = DownloadHandlerTexture.GetContent(uwr);
                if (texture != null)
                {
                    spriteCache[spriteId] = texture;
                }
                logger?.Log($"DownloadItemSpriteAsync success: {spriteId}", this);
                return texture;
            }
        }
    }
}
