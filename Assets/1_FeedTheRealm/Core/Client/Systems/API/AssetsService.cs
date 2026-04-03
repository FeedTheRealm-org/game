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
            $"http://{apiConfig.Hostname}:{apiConfig.Port}/assets/cosmetics";

        [System.Serializable]
        private class CosmeticResponse
        {
            public string cosmetic_id;
            public string cosmetic_url;
        }

        [System.Serializable]
        private class CosmeticsListResponse
        {
            public CosmeticResponse[] cosmetics_list;
        }

        private string GetCosmeticsCdnBaseUrl()
        {
            if (string.IsNullOrWhiteSpace(apiConfig.CosmeticsCDN))
                return string.Empty;

            var baseUrl = apiConfig.CosmeticsCDN.Trim();
            if (!baseUrl.StartsWith("http://") && !baseUrl.StartsWith("https://"))
            {
                baseUrl = $"http://{baseUrl}";
            }

            return baseUrl.TrimEnd('/');
        }

        private string BuildCosmeticsCdnUrl(string spriteUrl)
        {
            if (string.IsNullOrWhiteSpace(spriteUrl))
                return string.Empty;

            if (spriteUrl.StartsWith("http://") || spriteUrl.StartsWith("https://"))
            {
                return spriteUrl;
            }

            var path = spriteUrl.Trim();
            if (!path.StartsWith('/'))
            {
                path = $"/{path}";
            }

            var baseUrl = GetCosmeticsCdnBaseUrl();
            if (string.IsNullOrEmpty(baseUrl))
                return string.Empty;

            return $"{baseUrl}{path}";
        }

        private static SpriteResponse MapToSpriteResponse(CosmeticResponse cosmetic)
        {
            if (cosmetic == null)
                return null;

            return new SpriteResponse
            {
                sprite_id = cosmetic.cosmetic_id,
                sprite_url = cosmetic.cosmetic_url,
            };
        }

        private static SpritesListResponse MapToSpritesListResponse(CosmeticsListResponse cosmetics)
        {
            var sprites = new SpritesListResponse
            {
                sprites_list = new SpriteResponse[cosmetics?.cosmetics_list?.Length ?? 0],
            };

            if (cosmetics?.cosmetics_list == null)
                return sprites;

            for (int idx = 0; idx < cosmetics.cosmetics_list.Length; idx++)
            {
                sprites.sprites_list[idx] = MapToSpriteResponse(cosmetics.cosmetics_list[idx]);
            }

            return sprites;
        }

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
                var res = JsonUtility.FromJson<DataEnvelope<CosmeticsListResponse>>(responseText);
                handler?.Invoke(MapToSpritesListResponse(res.data), "");
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
                var res = JsonUtility.FromJson<DataEnvelope<CosmeticsListResponse>>(responseText);
                return MapToSpritesListResponse(res.data);
            }
        }

        /// <summary>
        /// Retrieve one sprite metadata record by ID.
        /// </summary>
        public async Task<SpriteResponse> GetSpriteByIdAsync(string spriteId)
        {
            var url = $"{GetBaseUrl()}/{spriteId}";
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
                    $"GetSpriteById error: {(res != null ? $"{res.title}: {res.detail}" : responseText)} - {responseText}",
                    this,
                    Logging.LogType.Error
                );
                return null;
            }

            var envelope = JsonUtility.FromJson<DataEnvelope<CosmeticResponse>>(responseText);
            return MapToSpriteResponse(envelope.data);
        }

        /// <summary>
        /// Download a sprite texture by sprite URL (or by sprite ID for backward compatibility).
        /// </summary>
        public async Task<Texture2D> DownloadTexture2D(string spriteReference)
        {
            if (string.IsNullOrWhiteSpace(spriteReference))
            {
                logger.Log(
                    "DownloadTexture2D called with empty sprite reference",
                    this,
                    Logging.LogType.Warning
                );
                return null;
            }

            string spriteUrl = spriteReference;
            if (System.Guid.TryParse(spriteReference, out _))
            {
                var sprite = await GetSpriteByIdAsync(spriteReference);
                if (sprite == null || string.IsNullOrWhiteSpace(sprite.sprite_url))
                {
                    logger.Log(
                        $"DownloadTexture2D could not resolve sprite URL for id: {spriteReference}",
                        this,
                        Logging.LogType.Error
                    );
                    return null;
                }

                spriteUrl = sprite.sprite_url;
            }

            var url = BuildCosmeticsCdnUrl(spriteUrl);
            if (string.IsNullOrEmpty(url))
            {
                logger.Log(
                    $"DownloadTexture2D invalid CosmeticsCDN or sprite URL. CosmeticsCDN: '{apiConfig.CosmeticsCDN}', sprite_url: '{spriteUrl}'",
                    this,
                    Logging.LogType.Error
                );
                return null;
            }

            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
            {
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

                logger.Log($"DownloadTexture2D success for sprite_url: {spriteUrl}", this);

                return texture;
            }
        }
    }
}
