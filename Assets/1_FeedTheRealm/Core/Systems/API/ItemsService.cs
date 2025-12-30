using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace API
{
    /// <summary>
    /// Service to manage items metadata downloading from API.
    /// Follows the same pattern as AssetsService.
    /// Only handles visual metadata (displayName, sprites, etc).
    /// Server-side gameplay data is handled separately.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemsService", menuName = "Scriptable Objects/API/ItemsService")]
    public class ItemsService : ScriptableObject
    {
        [Header("Server settings")]
        [SerializeField]
        public string Hostname;

        [SerializeField]
        public int Port;

        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;

        /// <summary>
        /// Retrieve all items metadata from API.
        /// This is called once on initialization to populate the cache.
        /// </summary>
        public IEnumerator GetItemsMetadata(System.Action<ItemsListResponse, string> handler)
        {
            var url = $"http://{Hostname}:{Port}/items/metadata";

            logger?.Log($"GetItemsMetadata - URL: {url}", this);

            var uwr = new UnityWebRequest(url, "GET");
            uwr.downloadHandler = new DownloadHandlerBuffer();

            yield return uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

            if (
                uwr.result == UnityWebRequest.Result.ConnectionError
                || uwr.result == UnityWebRequest.Result.ProtocolError
            )
            {
                logger?.Log(
                    $"GetItemsMetadata HTTP Error - Status: {uwr.responseCode}, Error: {uwr.error}",
                    this,
                    Logging.LogType.Error
                );
                logger?.Log(
                    $"GetItemsMetadata Response Text: {responseText}",
                    this,
                    Logging.LogType.Error
                );

                var res = string.IsNullOrEmpty(responseText)
                    ? null
                    : JsonUtility.FromJson<ErrorResponse>(responseText);
                logger?.Log(
                    $"GetItemsMetadata error: {(res != null ? $"{res.title}: {res.detail}" : responseText)}",
                    this,
                    Logging.LogType.Error
                );
                handler?.Invoke(null, res?.detail ?? $"HTTP {uwr.responseCode}: {uwr.error}");
            }
            else
            {
                var res = JsonUtility.FromJson<DataEnvelope<ItemsListResponse>>(responseText);
                logger?.Log(
                    $"GetItemsMetadata success: {res.data.items.Length} items loaded",
                    this
                );
                handler?.Invoke(res.data, "");
            }
        }

        /// <summary>
        /// Retrieve single item metadata by ID.
        /// Used for lazy loading individual items if needed.
        /// </summary>
        public IEnumerator GetItemById(
            string itemId,
            System.Action<ItemMetadataResponse, string> handler
        )
        {
            var url = $"http://{Hostname}:{Port}/items/{itemId}";
            var uwr = new UnityWebRequest(url, "GET");
            uwr.downloadHandler = new DownloadHandlerBuffer();

            yield return uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

            if (
                uwr.result == UnityWebRequest.Result.ConnectionError
                || uwr.result == UnityWebRequest.Result.ProtocolError
            )
            {
                var res = string.IsNullOrEmpty(responseText)
                    ? null
                    : JsonUtility.FromJson<ErrorResponse>(responseText);
                logger?.Log(
                    $"GetItemById error for {itemId}: {(res != null ? $"{res.title}: {res.detail}" : responseText)}",
                    this,
                    Logging.LogType.Error
                );
                handler?.Invoke(null, res?.detail ?? "Unknown error");
            }
            else
            {
                var res = JsonUtility.FromJson<DataEnvelope<ItemMetadataResponse>>(responseText);
                logger?.Log($"GetItemById success: {itemId}", this);
                handler?.Invoke(res.data, "");
            }
        }
    }
}
