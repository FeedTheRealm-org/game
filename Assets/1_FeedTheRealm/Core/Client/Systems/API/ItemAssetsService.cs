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

        [Header("General settings")]
        [SerializeField]
        private Logging.Logger logger;
        private string currentWorldId;

        // Map to store multiple categories by name
        private readonly System.Collections.Generic.Dictionary<string, string> categoriesMap =
            new System.Collections.Generic.Dictionary<string, string>();

        [Serializable]
        private class CategoryItem
        {
            public string category_id;
            public string category_name;
        }

        [Serializable]
        private class CategoriesData
        {
            public CategoryItem[] category_list;
        }

        [Serializable]
        private class CategoriesResponse
        {
            public CategoriesData data;
        }

        private string GetBaseUrl() => $"http://{apiConfig.Hostname}:{apiConfig.Port}/assets/items";

        private string GetCdnUrl(string categoryId) =>
            $"http://{apiConfig.ModelsCDN}/worlds/{currentWorldId}/items/categories/{categoryId}";

        // Simple in-memory cache to avoid downloading the same sprite multiple times
        private readonly System.Collections.Generic.Dictionary<string, Texture2D> spriteCache =
            new System.Collections.Generic.Dictionary<string, Texture2D>();

        /// <summary>
        /// Download sprite by spriteId.
        /// Full URL: /assets/items/{spriteId}
        /// </summary>
        public async System.Threading.Tasks.Task<Texture2D> DownloadItemSpriteAsync(
            string spriteId,
            string categoryName = "consumables"
        )
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

            // Get category ID from map
            string categoryId = "";
            if (categoriesMap.TryGetValue(categoryName, out var mappedId))
            {
                categoryId = mappedId;
            }
            else if (categoriesMap.Count > 0)
            {
                // Fallback to first available category if requested one doesn't exist
                foreach (var kvp in categoriesMap)
                {
                    categoryId = kvp.Value;
                    break;
                }
                Debug.LogWarning(
                    $"[ItemAssetsService] Category '{categoryName}' not found. Falling back to '{categoryId}'"
                );
            }

            if (string.IsNullOrEmpty(categoryId))
            {
                Debug.LogError("[ItemAssetsService] Cannot download sprite: no categories loaded.");
                return null;
            }

            // Extract just the filename from full path, e.g. "Sprites/uuid.png" -> "uuid.png"
            string fileName = System.IO.Path.GetFileName(spriteId);

            var url = $"{GetCdnUrl(categoryId)}/{fileName}";
            Debug.Log(
                $"[ItemAssetsService] Current WorldID: {currentWorldId} | Current CategoryID: {categoryId}"
            );
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
        /// Get item categories.
        /// Full URL: /assets/items/categories
        /// </summary>
        public async System.Threading.Tasks.Task<string> GetCategoriesAsync()
        {
            var url = $"{GetBaseUrl()}/categories";
            Debug.Log($"[ItemAssetsService] GetCategoriesAsync fetching from URL: {url}");
            using var uwr = UnityWebRequest.Get(url);

            var asyncOp = uwr.SendWebRequest();
            await asyncOp;

            if (
                uwr.result == UnityWebRequest.Result.ConnectionError
                || uwr.result == UnityWebRequest.Result.ProtocolError
            )
            {
                Debug.LogError(
                    $"[ItemAssetsService] GetCategoriesAsync error: {uwr.error} | Response code: {uwr.responseCode}"
                );
                logger?.Log($"GetCategoriesAsync error: {uwr.error}", this, Logging.LogType.Error);
                return null;
            }

            logger?.Log("GetCategoriesAsync success", this);
            return uwr.downloadHandler.text;
        }

        /// <summary>
        /// Initializes the service by setting the current world ID and fetching the global category.
        /// Call this once when the world is loaded to set the category ID for subsequent texture downloads.
        /// </summary>
        public async System.Threading.Tasks.Task InitializeCategoryForWorldAsync(string worldId)
        {
            currentWorldId = worldId;
            Debug.Log($"[ItemAssetsService] Fetching categories for world {worldId}...");
            string categoriesJson = await GetCategoriesAsync();

            if (!string.IsNullOrEmpty(categoriesJson))
            {
                Debug.Log($"[ItemAssetsService] Raw categories JSON received: {categoriesJson}");
                try
                {
                    var response = JsonUtility.FromJson<CategoriesResponse>(categoriesJson);
                    if (
                        response != null
                        && response.data != null
                        && response.data.category_list != null
                        && response.data.category_list.Length > 0
                    )
                    {
                        categoriesMap.Clear();
                        foreach (var cat in response.data.category_list)
                        {
                            categoriesMap[cat.category_name] = cat.category_id;
                            Debug.Log(
                                $"[ItemAssetsService] Cached Category: {cat.category_name} -> {cat.category_id}"
                            );
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            "[ItemAssetsService] Failed to parse categories, or list is empty."
                        );
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError(
                        $"[ItemAssetsService] Could not parse categories JSON: {ex.Message}"
                    );
                    logger?.Log(
                        $"Could not parse categories JSON: {ex.Message}",
                        this,
                        Logging.LogType.Error
                    );
                }
            }
            else
            {
                Debug.LogWarning("[ItemAssetsService] Categories JSON was null or empty.");
            }
        }
    }
}
