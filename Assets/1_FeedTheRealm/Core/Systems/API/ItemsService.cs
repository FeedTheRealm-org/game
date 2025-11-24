using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace API {
    /// <summary>
    /// Service to manage items metadata downloading from API.
    /// Follows the same pattern as AssetsService.
    /// Only handles visual metadata (displayName, sprites, etc).
    /// Server-side gameplay data is handled separately.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemsService", menuName = "Scriptable Objects/API/ItemsService")]
    public class ItemsService : ScriptableObject {
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
        private string dedicatedServerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3NjQwMjE5NjksImlzcyI6MTc2MzkzNTU2OSwidXNlcklEIjoiY2QxMjNkNjktMWE5MS00ZjQxLTlhOTQtYjE3YjQyMmQzOWRlIn0.QSr9jfdAKAEPVrhzZINEVRX8mFB6asjpPjDxAEJI-4Q";

        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        /// <summary>
        /// Retrieve all items metadata from API.
        /// This is called once on initialization to populate the cache.
        /// </summary>
        public IEnumerator GetItemsMetadata(System.Action<ItemsListResponse, string> handler) {
            var url = $"http://{Hostname}:{Port}/api/items/metadata";
            
            logger?.Log($"GetItemsMetadata - URL: {url}", this);
            
            var uwr = new UnityWebRequest(url, "GET");
            uwr.downloadHandler = new DownloadHandlerBuffer();

            // Determine which token to use
            string token = GetAuthToken();
            
            if (!string.IsNullOrEmpty(token)) {
                uwr.SetRequestHeader("Authorization", $"Bearer {token}");
                logger?.Log($"GetItemsMetadata - Using token (length: {token.Length})", this);
            } else {
                logger?.Log("WARNING: No authentication token available! Request will likely fail.", this, Logging.LogType.Warning);
            }

            yield return uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

            if (uwr.result == UnityWebRequest.Result.ConnectionError || 
                uwr.result == UnityWebRequest.Result.ProtocolError) {
                logger?.Log($"GetItemsMetadata HTTP Error - Status: {uwr.responseCode}, Error: {uwr.error}", this, Logging.LogType.Error);
                logger?.Log($"GetItemsMetadata Response Text: {responseText}", this, Logging.LogType.Error);
                
                // Check for authentication errors
                if (uwr.responseCode == 401 || uwr.responseCode == 403) {
                    Debug.LogWarning("⚠️ [ItemsService] AUTHENTICATION FAILED! The JWT token may be expired or invalid.");
                    Debug.LogWarning("⚠️ [ItemsService] If using dedicated server, update the hardcoded token in ItemsService.cs");
                }
                
                var res = string.IsNullOrEmpty(responseText) 
                    ? null 
                    : JsonUtility.FromJson<ErrorResponse>(responseText);
                logger?.Log($"GetItemsMetadata error: {(res != null ? $"{res.title}: {res.detail}" : responseText)}", 
                          this, Logging.LogType.Error);
                handler?.Invoke(null, res?.detail ?? $"HTTP {uwr.responseCode}: {uwr.error}");
            } else {
                var res = JsonUtility.FromJson<DataEnvelope<ItemsListResponse>>(responseText);
                logger?.Log($"GetItemsMetadata success: {res.data.items.Length} items loaded", this);
                handler?.Invoke(res.data, "");
            }
        }

        /// <summary>
        /// Retrieve single item metadata by ID.
        /// Used for lazy loading individual items if needed.
        /// </summary>
        public IEnumerator GetItemById(string itemId, System.Action<ItemMetadataResponse, string> handler) {
            var url = $"http://{Hostname}:{Port}/api/items/{itemId}";
            var uwr = new UnityWebRequest(url, "GET");
            uwr.downloadHandler = new DownloadHandlerBuffer();

            string token = GetAuthToken();
            if (!string.IsNullOrEmpty(token)) {
                uwr.SetRequestHeader("Authorization", $"Bearer {token}");
            }

            yield return uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

            if (uwr.result == UnityWebRequest.Result.ConnectionError || 
                uwr.result == UnityWebRequest.Result.ProtocolError) {
                // Check for authentication errors
                if (uwr.responseCode == 401 || uwr.responseCode == 403) {
                    Debug.LogWarning("⚠️ [ItemsService] AUTHENTICATION FAILED on GetItemById!");
                }
                
                var res = string.IsNullOrEmpty(responseText) 
                    ? null 
                    : JsonUtility.FromJson<ErrorResponse>(responseText);
                logger?.Log($"GetItemById error for {itemId}: {(res != null ? $"{res.title}: {res.detail}" : responseText)}", 
                          this, Logging.LogType.Error);
                handler?.Invoke(null, res?.detail ?? "Unknown error");
            } else {
                var res = JsonUtility.FromJson<DataEnvelope<ItemMetadataResponse>>(responseText);
                logger?.Log($"GetItemById success: {itemId}", this);
                handler?.Invoke(res.data, "");
            }
        }

        /// <summary>
        /// Get authentication token. Uses session token if available, otherwise uses dedicated server token.
        /// </summary>
        private string GetAuthToken() {
            #if UNITY_SERVER
            // Dedicated server: use hardcoded token
            if (!string.IsNullOrEmpty(dedicatedServerToken)) {
                logger?.Log("Using dedicated server hardcoded token", this);
                return dedicatedServerToken;
            } else {
                Debug.LogWarning("[ItemsService] Dedicated server token is not set!");
                return null;
            }
            #else
            // Client: use session token
            if (session != null && !string.IsNullOrEmpty(session.APIToken)) {
                return session.APIToken;
            } else {
                Debug.LogWarning("[ItemsService] Session or APIToken is null/empty!");
                return null;
            }
            #endif
        }
    }
}
