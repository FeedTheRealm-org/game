using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace API
{
    /// <summary>
    /// Service to manage assets downloading.
    /// </summary>
    [CreateAssetMenu(fileName = "AssetsService", menuName = "Scriptable Objects/API/AssetsService")]
    public class AssetsService : ScriptableObject
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

        private string GetBaseUrl() =>
            $"http://{apiConfig.Hostname}:{apiConfig.Port}/assets/sprites";

        /// <summary>
        /// Retrieve the list of categories for sprites.
        /// </summary>
        public IEnumerator GetCategories(System.Action<SpriteCategoryListResponse, string> handler)
        {
            var url = $"{GetBaseUrl()}/categories";
            var uwr = new UnityWebRequest(url, "GET");
            uwr.downloadHandler = new DownloadHandlerBuffer();

            uwr.SetRequestHeader("Authorization", $"Bearer {session.APIToken}");

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
                logger.Log(
                    $"GetCategories error: {(res != null ? $"{res.title}: {res.detail}" : responseText)} - {responseText}",
                    this,
                    Logging.LogType.Error
                );
                handler?.Invoke(null, res.detail);
            }
            else
            {
                var res = JsonUtility.FromJson<DataEnvelope<SpriteCategoryListResponse>>(
                    responseText
                );
                handler?.Invoke(res.data, "");
            }
        }

        /// <summary>
        /// Retrieve the list of sprites for a given category.
        /// </summary>
        public IEnumerator GetSpritesByCategory(
            string categoryId,
            System.Action<SpritesListResponse, string> handler
        )
        {
            var url = $"{GetBaseUrl()}/categories/{categoryId}";
            var uwr = new UnityWebRequest(url, "GET");
            uwr.downloadHandler = new DownloadHandlerBuffer();

            uwr.SetRequestHeader("Authorization", $"Bearer {session.APIToken}");

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
                logger.Log(
                    $"GetSpritesByCategory error: {(res != null ? $"{res.title}: {res.detail}" : responseText)} - {responseText}",
                    this,
                    Logging.LogType.Error
                );
                handler?.Invoke(null, res.detail);
            }
            else
            {
                var res = JsonUtility.FromJson<DataEnvelope<SpritesListResponse>>(responseText);
                handler?.Invoke(res.data, "");
            }
        }

        /// <summary>
        /// Retrieve the list of categories for sprites asynchronously.
        /// </summary>
        public async Task<SpriteCategoryListResponse> GetCategoriesAsync()
        {
            var url = $"{GetBaseUrl()}/categories";
            var uwr = new UnityWebRequest(url, "GET");
            uwr.downloadHandler = new DownloadHandlerBuffer();

            uwr.SetRequestHeader("Authorization", $"Bearer {session.APIToken}");

            await uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

            if (
                uwr.result == UnityWebRequest.Result.ConnectionError
                || uwr.result == UnityWebRequest.Result.ProtocolError
            )
            {
                var res = string.IsNullOrEmpty(responseText)
                    ? null
                    : JsonUtility.FromJson<ErrorResponse>(responseText);
                logger.Log(
                    $"GetCategories error: {(res != null ? $"{res.title}: {res.detail}" : responseText)} - {responseText}",
                    this,
                    Logging.LogType.Warning
                );
                return null;
            }
            else
            {
                var res = JsonUtility.FromJson<DataEnvelope<SpriteCategoryListResponse>>(
                    responseText
                );
                return res.data;
            }
        }

        /// <summary>
        /// Retrieve the list of sprites for a given category asynchronously.
        /// </summary>
        public async Task<SpritesListResponse> GetSpritesByCategoryAsync(string categoryId)
        {
            var url = $"{GetBaseUrl()}/categories/{categoryId}";
            var uwr = new UnityWebRequest(url, "GET");
            uwr.downloadHandler = new DownloadHandlerBuffer();

            uwr.SetRequestHeader("Authorization", $"Bearer {session.APIToken}");

            await uwr.SendWebRequest();

            var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;

            if (
                uwr.result == UnityWebRequest.Result.ConnectionError
                || uwr.result == UnityWebRequest.Result.ProtocolError
            )
            {
                var res = string.IsNullOrEmpty(responseText)
                    ? null
                    : JsonUtility.FromJson<ErrorResponse>(responseText);
                logger.Log(
                    $"GetSpritesByCategory error: {(res != null ? $"{res.title}: {res.detail}" : responseText)} - {responseText}",
                    this,
                    Logging.LogType.Error
                );
                return null;
            }
            else
            {
                var res = JsonUtility.FromJson<DataEnvelope<SpritesListResponse>>(responseText);
                return res.data;
            }
        }

        /// <summary>
        /// Download the sprite with the given id.
        /// </summary>
        public async Task<Texture2D> DownloadTexture2D(string spriteId)
        {
            var url = $"{GetBaseUrl()}/{spriteId}";

            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
            {
                uwr.SetRequestHeader("Authorization", $"Bearer {session.APIToken}");

                await uwr.SendWebRequest();

                if (
                    uwr.result == UnityWebRequest.Result.ConnectionError
                    || uwr.result == UnityWebRequest.Result.ProtocolError
                )
                {
                    var responseText = uwr.downloadHandler?.text ?? uwr.error ?? string.Empty;
                    var res = string.IsNullOrEmpty(responseText)
                        ? null
                        : JsonUtility.FromJson<ErrorResponse>(responseText);

                    logger.Log(
                        $"DownloadTexture2D error: {(res != null ? $"{res.title}: {res.detail}" : responseText)}",
                        this,
                        Logging.LogType.Error
                    );

                    return null;
                }

                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                logger.Log($"DownloadTexture2D success for sprite_id: {spriteId}", this);

                return texture;
            }
        }
    }
}
