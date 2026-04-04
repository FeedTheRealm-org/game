using System;
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
        [Header("API Config")]
        [SerializeField]
        private ApiConfig apiConfig;

        [Header("Session settings")]
        [SerializeField]
        private Session.Session session;

        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        private string currentWorldId;

        private string GetBaseUrl() => $"http://{apiConfig.Hostname}:{apiConfig.Port}/assets/items";

        private string GetBaseCdnUrl() => $"http://{apiConfig.ModelsCDN}/worlds";

        // Simple in-memory cache to avoid downloading the same sprite multiple times
        private readonly System.Collections.Generic.Dictionary<string, Texture2D> spriteCache =
            new System.Collections.Generic.Dictionary<string, Texture2D>();

        private void OnEnable()
        {
            spriteCache.Clear();
        }

        /// <summary>
        /// Download sprite by spriteId.
        /// Full URL: /assets/items/{spriteId}
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
                await System.Threading.Tasks.Task.Yield();
                return cachedTexture;
            }

            // Extract just the filename from full path, e.g. "Sprites/uuid.png" -> "uuid.png"
            string fileName = System.IO.Path.GetFileName(spriteId);

            var url = $"{GetBaseCdnUrl()}/{currentWorldId}/items/{fileName}";
            Debug.Log($"[ItemAssetsService] Current WorldID: {currentWorldId}");
            Debug.Log($"[ItemAssetsService] DownloadItemSpriteAsync fetching from URL: {url}");
            logger?.Log($"DownloadItemSpriteAsync fetching from URL: {url}", this);
            using var uwr = UnityWebRequestTexture.GetTexture(url);
            var asyncOp = uwr.SendWebRequest();
            await asyncOp;

            if (
                uwr.result == UnityWebRequest.Result.ConnectionError
                || uwr.result == UnityWebRequest.Result.ProtocolError
            )
            {
                Debug.LogError($"[ItemAssetsService] error for {spriteId}: {uwr.error}");
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

        /// <summary>
        /// Initializes the service by setting the current world ID and fetching the global category.
        /// Call this once when the world is loaded to set the category ID for subsequent texture downloads.
        /// </summary>
        public async System.Threading.Tasks.Task SetCurrentWorldId(string worldId)
        {
            currentWorldId = worldId;
        }
    }
}
