using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using API;

namespace Items {
    /// <summary>
    /// Manager for items metadata on the dedicated server.
    /// Only loads metadata (no sprites needed on server).
    /// Used to configure loot drops from enemies.
    /// </summary>
    public class DedicatedServerItemsManager : MonoBehaviour {
        [Header("API Services")]
        [SerializeField]
        private ItemsService itemsService;

        [Header("Debug")]
        [SerializeField]
        private bool enableDebugLogs = true;

        // Singleton instance
        public static DedicatedServerItemsManager Instance { get; private set; }

        // Cache for metadata only (no sprites on server)
        private Dictionary<string, ItemMetadataResponse> itemsById;
        private Dictionary<string, List<ItemMetadataResponse>> itemsByCategory;

        // Initialization state
        public bool IsInitialized { get; private set; }
        public int TotalItemsLoaded { get; private set; }

        void Awake() {
            // Singleton pattern
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize collections
            itemsById = new Dictionary<string, ItemMetadataResponse>();
            itemsByCategory = new Dictionary<string, List<ItemMetadataResponse>>();
        }

        /// <summary>
        /// Initialize the server items manager by loading metadata from API.
        /// Call this from ServerBootstrap after server starts.
        /// </summary>
        public IEnumerator Initialize() {
            if (IsInitialized) {
                DebugLog("Already initialized");
                yield break;
            }

            DebugLog("Initializing DedicatedServerItemsManager...");

            // Load all items metadata
            yield return LoadItemsMetadata();

            IsInitialized = true;
            DebugLog($"DedicatedServerItemsManager initialized! Total items: {TotalItemsLoaded}");
        }

        /// <summary>
        /// Load all items metadata from API.
        /// </summary>
        IEnumerator LoadItemsMetadata() {
            bool completed = false;

            yield return itemsService.GetItemsMetadata((itemsList, error) => {
                if (!string.IsNullOrEmpty(error)) {
                    Debug.LogError($"[DedicatedServerItemsManager] Failed to load items metadata: {error}");
                    completed = true;
                    return;
                }

                // Build dictionaries from array
                itemsById.Clear();
                itemsByCategory.Clear();

                foreach (var item in itemsList.items) {
                    itemsById[item.id] = item;

                    // Group by category for easy filtering
                    if (!itemsByCategory.ContainsKey(item.category)) {
                        itemsByCategory[item.category] = new List<ItemMetadataResponse>();
                    }
                    itemsByCategory[item.category].Add(item);
                }

                TotalItemsLoaded = itemsById.Count;
                DebugLog($"Loaded {TotalItemsLoaded} items metadata");
                
                // Log categories for debugging
                foreach (var category in itemsByCategory.Keys) {
                    DebugLog($"  Category '{category}': {itemsByCategory[category].Count} items");
                }

                completed = true;
            });

            yield return new WaitUntil(() => completed);
        }

        /// <summary>
        /// Get item metadata by ID. Returns null if not found.
        /// </summary>
        public ItemMetadataResponse GetItemById(string itemId) {
            if (itemsById.TryGetValue(itemId, out var item)) {
                return item;
            }
            Debug.LogWarning($"[DedicatedServerItemsManager] Item not found: {itemId}");
            return null;
        }

        /// <summary>
        /// Get all items metadata.
        /// </summary>
        public ItemMetadataResponse[] GetAllItems() {
            var items = new ItemMetadataResponse[itemsById.Count];
            itemsById.Values.CopyTo(items, 0);
            return items;
        }

        /// <summary>
        /// Get all item IDs.
        /// </summary>
        public List<string> GetAllItemIds() {
            return new List<string>(itemsById.Keys);
        }

        /// <summary>
        /// Get items by category.
        /// </summary>
        public List<ItemMetadataResponse> GetItemsByCategory(string category) {
            if (itemsByCategory.TryGetValue(category, out var items)) {
                return new List<ItemMetadataResponse>(items);
            }
            return new List<ItemMetadataResponse>();
        }

        /// <summary>
        /// Get all item IDs by category.
        /// </summary>
        public List<string> GetItemIdsByCategory(string category) {
            var items = GetItemsByCategory(category);
            var ids = new List<string>();
            foreach (var item in items) {
                ids.Add(item.id);
            }
            return ids;
        }

        /// <summary>
        /// Get random item ID from all items.
        /// </summary>
        public string GetRandomItemId() {
            if (itemsById.Count == 0) {
                Debug.LogWarning("[DedicatedServerItemsManager] No items available");
                return null;
            }

            var allIds = GetAllItemIds();
            int randomIndex = Random.Range(0, allIds.Count);
            return allIds[randomIndex];
        }

        /// <summary>
        /// Get random item ID from specific category.
        /// </summary>
        public string GetRandomItemIdFromCategory(string category) {
            var categoryIds = GetItemIdsByCategory(category);
            if (categoryIds.Count == 0) {
                Debug.LogWarning($"[DedicatedServerItemsManager] No items in category: {category}");
                return null;
            }

            int randomIndex = Random.Range(0, categoryIds.Count);
            return categoryIds[randomIndex];
        }

        /// <summary>
        /// Get multiple random item IDs.
        /// </summary>
        public List<string> GetRandomItemIds(int count) {
            var allIds = GetAllItemIds();
            if (allIds.Count == 0) return new List<string>();

            var result = new List<string>();
            for (int i = 0; i < count; i++) {
                int randomIndex = Random.Range(0, allIds.Count);
                result.Add(allIds[randomIndex]);
            }
            return result;
        }

        /// <summary>
        /// Check if item exists.
        /// </summary>
        public bool HasItem(string itemId) {
            return itemsById.ContainsKey(itemId);
        }

        /// <summary>
        /// Get available categories.
        /// </summary>
        public List<string> GetCategories() {
            return new List<string>(itemsByCategory.Keys);
        }

        /// <summary>
        /// Reload metadata from API (for detecting new items without restart).
        /// </summary>
        public IEnumerator ReloadMetadata() {
            DebugLog("Reloading items metadata...");
            yield return LoadItemsMetadata();
            DebugLog($"Metadata reloaded: {TotalItemsLoaded} items");
        }

        void DebugLog(string message) {
            if (enableDebugLogs) {
                Debug.Log($"[DedicatedServerItemsManager] {message}");
            }
        }
    }
}
