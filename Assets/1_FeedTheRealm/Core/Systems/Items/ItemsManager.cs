using System.Collections;
using System.Collections.Generic;
using API;
using UnityEngine;

namespace Items
{
    /// <summary>
    /// Central manager for item metadata and sprite caching.
    /// Handles initialization, caching, and providing access to item data.
    /// Use this as a singleton to manage all item-related operations.
    /// </summary>
    public class ItemsManager : MonoBehaviour
    {
        [Header("API Services")]
        [SerializeField]
        private ItemsService itemsService;

        [SerializeField]
        private ItemAssetsService itemAssetsService;

        [Header("Settings")]
        [SerializeField]
        [Tooltip(
            "Always preload all sprites on initialization (recommended for small sprite sets)"
        )]
        private bool preloadAllSprites = true;

        [Header("Debug")]
        [SerializeField]
        private bool enableDebugLogs = true;

        // Singleton instance
        public static ItemsManager Instance { get; private set; }

        // Cache dictionaries
        private Dictionary<string, ItemMetadataResponse> itemsById;
        private Dictionary<string, Texture2D> spriteCache;
        private HashSet<string> loadingSprites; // Track sprites currently loading

        // Initialization state
        public bool IsInitialized { get; private set; }
        public int TotalItemsLoaded { get; private set; }
        public int TotalSpritesLoaded { get; private set; }

        void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // NOTE: DontDestroyOnLoad is called by parent ItemsManagerBootstrap

            // Initialize collections
            itemsById = new Dictionary<string, ItemMetadataResponse>();
            spriteCache = new Dictionary<string, Texture2D>();
            loadingSprites = new HashSet<string>();
        }

        void Start()
        {
            StartCoroutine(Initialize());
        }

        /// <summary>
        /// Initialize the items manager.
        ///
        /// Legacy metadata-based loading has been disabled in favor of
        /// world-driven items (WorldItemsRegistry + ItemAssetsService).
        /// This initializer now simply marks the manager as ready so
        /// existing callers that wait on IsInitialized keep working.
        /// </summary>
        IEnumerator Initialize()
        {
            if (IsInitialized)
                yield break;

            DebugLog(
                "Initializing ItemsManager in world-driven mode (metadata loading disabled)..."
            );

            // Do NOT load metadata from ItemsService anymore.
            // World items are defined in WorldData.consumableItems and
            // accessed via Worlds.WorldItemsRegistry.
            itemsById.Clear();
            TotalItemsLoaded = 0;
            TotalSpritesLoaded = 0;

            IsInitialized = true;
            DebugLog(
                "ItemsManager initialized. Legacy metadata system is disabled; "
                    + "runtime items are driven by WorldData.consumableItems."
            );
        }

        /// <summary>
        /// Load all items metadata from API.
        ///
        /// This method is kept for API compatibility but is now a no-op,
        /// as gameplay items are fully driven by WorldData instead of
        /// the legacy items metadata list.
        /// </summary>
        IEnumerator LoadItemsMetadata()
        {
            itemsById.Clear();
            TotalItemsLoaded = 0;
            DebugLog(
                "Skipping legacy items metadata loading; world-driven items are used instead."
            );
            yield break;
        }

        /// <summary>
        /// Get item metadata by ID. Returns null if not found.
        /// </summary>
        public ItemMetadataResponse GetItemById(string itemId)
        {
            if (itemsById.TryGetValue(itemId, out var item))
            {
                return item;
            }
            Debug.LogWarning($"[ItemsManager] Item not found: {itemId}");
            return null;
        }

        /// <summary>
        /// Get all items metadata.
        /// </summary>
        public ItemMetadataResponse[] GetAllItems()
        {
            var items = new ItemMetadataResponse[itemsById.Count];
            itemsById.Values.CopyTo(items, 0);
            return items;
        }

        /// <summary>
        /// Get item sprite with lazy loading and caching.
        /// If sprite is not cached, it will be downloaded automatically.
        /// </summary>
        public IEnumerator GetItemSprite(string itemId, System.Action<Texture2D> callback)
        {
            // Check if sprite is already cached
            if (spriteCache.TryGetValue(itemId, out var cachedTexture))
            {
                callback?.Invoke(cachedTexture);
                yield break;
            }

            // Check if item exists
            if (!itemsById.TryGetValue(itemId, out var item))
            {
                Debug.LogWarning($"[ItemsManager] Cannot get sprite: Item {itemId} not found");
                callback?.Invoke(null);
                yield break;
            }

            // Check if already loading
            if (loadingSprites.Contains(itemId))
            {
                // Wait until loading completes
                yield return new WaitUntil(() => !loadingSprites.Contains(itemId));
                callback?.Invoke(spriteCache.ContainsKey(itemId) ? spriteCache[itemId] : null);
                yield break;
            }

            // Mark as loading
            loadingSprites.Add(itemId);

            // Download sprite using sprite id route
            bool completed = false;
            yield return itemAssetsService.DownloadItemSprite(
                item.sprite_id,
                (texture) =>
                {
                    if (texture != null)
                    {
                        spriteCache[itemId] = texture;
                        TotalSpritesLoaded++;
                        DebugLog($"Sprite loaded for item: {item.name} ({itemId})");
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[ItemsManager] Failed to load sprite for item: {item.name}"
                        );
                    }
                    callback?.Invoke(texture);
                    completed = true;
                }
            );

            yield return new WaitUntil(() => completed);
            loadingSprites.Remove(itemId);
        }

        /// <summary>
        /// Preload all sprites for all items.
        /// </summary>
        IEnumerator PreloadAllSprites()
        {
            foreach (var item in itemsById.Values)
            {
                yield return GetItemSprite(item.id, null);
                yield return null; // Small delay to avoid frame drops
            }
        }

        /// <summary>
        /// Clear sprite cache to free memory.
        /// Metadata is retained.
        /// </summary>
        public void ClearSpriteCache()
        {
            spriteCache.Clear();
            TotalSpritesLoaded = 0;
            DebugLog("Sprite cache cleared");
        }

        /// <summary>
        /// Reload all metadata from API.
        ///
        /// In world-driven mode this is effectively a no-op and only
        /// clears the local legacy cache to keep behavior predictable.
        /// </summary>
        public IEnumerator ReloadMetadata()
        {
            DebugLog(
                "ReloadMetadata called, but legacy metadata loading is disabled. "
                    + "Clearing local cache only."
            );
            itemsById.Clear();
            TotalItemsLoaded = 0;
            yield break;
        }

        void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[ItemsManager] {message}");
            }
        }

        #region Public Helpers

        /// <summary>
        /// Check if a sprite is already cached.
        /// </summary>
        public bool IsSpriteLoaded(string itemId)
        {
            return spriteCache.ContainsKey(itemId);
        }

        /// <summary>
        /// Get sprite synchronously if already cached. Returns null if not cached.
        /// </summary>
        public Texture2D GetCachedSprite(string itemId)
        {
            return spriteCache.TryGetValue(itemId, out var texture) ? texture : null;
        }

        #endregion
    }
}
