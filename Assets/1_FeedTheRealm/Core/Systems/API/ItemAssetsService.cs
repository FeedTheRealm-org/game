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
        public IEnumerator DownloadItemSprite(string spriteId, System.Action<Texture2D> handler)
        {
            if (string.IsNullOrEmpty(spriteId))
            {
                logger?.Log(
                    "DownloadItemSprite called with null or empty spriteId",
                    this,
                    Logging.LogType.Warning
                );
                handler?.Invoke(null);
                yield break;
            }

            // If already cached, return immediately without doing a web request
            if (spriteCache.TryGetValue(spriteId, out var cachedTexture))
            {
                logger?.Log($"DownloadItemSprite cache hit: {spriteId}", this);
                handler?.Invoke(cachedTexture);
                yield break;
            }

            var url = $"http://{Hostname}:{Port}/assets/sprites/items/{spriteId}";
            var uwr = UnityWebRequestTexture.GetTexture(url);

            yield return uwr.SendWebRequest();

            if (
                uwr.result == UnityWebRequest.Result.ConnectionError
                || uwr.result == UnityWebRequest.Result.ProtocolError
            )
            {
                logger?.Log(
                    $"DownloadItemSprite error for {spriteId}: {uwr.error}",
                    this,
                    Logging.LogType.Error
                );
                handler?.Invoke(null);
            }
            else
            {
                var texture = DownloadHandlerTexture.GetContent(uwr);
                if (texture != null)
                {
                    spriteCache[spriteId] = texture;
                }
                logger?.Log($"DownloadItemSprite success: {spriteId}", this);
                handler?.Invoke(texture);
            }
        }
    }
}
