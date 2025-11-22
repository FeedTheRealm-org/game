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

        [Header("Session settings")]
        [SerializeField]
        private Session.Session session;

        [Header("Dedicated Server Settings")]
        [SerializeField]
        [Tooltip("Hardcoded JWT token for dedicated server (fallback when session is not available)")]
        private string dedicatedServerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3NjM4NDc5NjQsImlzcyI6MTc2Mzc2MTU2NCwidXNlcklEIjoiY2QxMjNkNjktMWE5MS00ZjQxLTlhOTQtYjE3YjQyMmQzOWRlIn0.Er3_zi3U0gJ2AYi9e4wE8bIwq0CwgrgKbV1yr6JEZqo";

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

            string token = GetAuthToken();
            if (!string.IsNullOrEmpty(token)) {
                uwr.SetRequestHeader("Authorization", $"Bearer {token}");
            } else {
                logger?.Log("WARNING: No authentication token available for sprite download!", this, Logging.LogType.Warning);
            }

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError || 
                uwr.result == UnityWebRequest.Result.ProtocolError) {
                if (uwr.responseCode == 401 || uwr.responseCode == 403) {
                    Debug.LogWarning("⚠️ [ItemAssetsService] AUTHENTICATION FAILED on sprite download!");
                }
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

            string token = GetAuthToken();
            if (!string.IsNullOrEmpty(token)) {
                uwr.SetRequestHeader("Authorization", $"Bearer {token}");
            } else {
                logger?.Log("WARNING: No authentication token available for sprite download!", this, Logging.LogType.Warning);
            }

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError || 
                uwr.result == UnityWebRequest.Result.ProtocolError) {
                if (uwr.responseCode == 401 || uwr.responseCode == 403) {
                    Debug.LogWarning("⚠️ [ItemAssetsService] AUTHENTICATION FAILED on sprite download by category!");
                }
                logger?.Log($"DownloadItemSpriteByCategory error for {category}/{spriteId}: {uwr.error}", 
                          this, Logging.LogType.Error);
                handler?.Invoke(null);
            } else {
                var texture = DownloadHandlerTexture.GetContent(uwr);
                logger?.Log($"DownloadItemSpriteByCategory success: {category}/{spriteId}", this);
                handler?.Invoke(texture);
            }
        }

        /// <summary>
        /// Get authentication token. Uses session token if available, otherwise uses dedicated server token.
        /// Note: Dedicated server typically doesn't download sprites, but this is here for consistency.
        /// </summary>
        private string GetAuthToken() {
            #if UNITY_SERVER
            // Dedicated server: use hardcoded token
            if (!string.IsNullOrEmpty(dedicatedServerToken)) {
                return dedicatedServerToken;
            } else {
                Debug.LogWarning("[ItemAssetsService] Dedicated server token is not set!");
                return null;
            }
            #else
            // Client: use session token
            if (session != null && !string.IsNullOrEmpty(session.APIToken)) {
                return session.APIToken;
            } else {
                Debug.LogWarning("[ItemAssetsService] Session or APIToken is null/empty!");
                return null;
            }
            #endif
        }
    }
}
