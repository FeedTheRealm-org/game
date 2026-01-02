using System.Collections;
using System.Collections.Generic;
using API;
using UnityEngine;

namespace Items
{
    /// <summary>
    /// Manager for items metadata on the dedicated server.
    /// Only loads metadata (no sprites needed on server).
    /// Used to configure loot drops from enemies.
    /// </summary>
    public class DedicatedServerItemsManager : MonoBehaviour
    {
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

        // Initialization state
        public bool IsInitialized { get; private set; }
        public int TotalItemsLoaded { get; private set; }

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
        }

        /// <summary>
        /// Initialize the server items manager.
        ///
        /// Legacy metadata-based loading has been disabled. Loot on the
        /// dedicated server is now fully driven by WorldData (see
        /// Worlds.WorldItemsRegistry and LootDropper).
        /// </summary>
        public IEnumerator Initialize()
        {
            if (IsInitialized)
            {
                DebugLog("Already initialized");
                yield break;
            }

            DebugLog(
                "Initializing DedicatedServerItemsManager in world-driven mode (metadata disabled)..."
            );

            // Do NOT call the metadata API anymore.
            itemsById.Clear();
            TotalItemsLoaded = 0;

            IsInitialized = true;
            DebugLog(
                "DedicatedServerItemsManager initialized. Legacy metadata system is disabled; "
                    + "server loot is driven by WorldData.enemies[*].lootItems."
            );
        }

        /// <summary>
        /// Load all items metadata from API.
        ///
        /// Kept for API compatibility but now a no-op, as the server
        /// uses world-driven loot instead of the legacy metadata list.
        /// </summary>
        IEnumerator LoadItemsMetadata()
        {
            itemsById.Clear();
            TotalItemsLoaded = 0;
            DebugLog(
                "Skipping legacy items metadata loading on server; world-driven loot is used instead."
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
            Debug.LogWarning($"[DedicatedServerItemsManager] Item not found: {itemId}");
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
        /// Get all item IDs.
        /// </summary>
        public List<string> GetAllItemIds()
        {
            return new List<string>(itemsById.Keys);
        }

        /// <summary>
        /// Get random item ID from all items.
        /// </summary>
        public string GetRandomItemId()
        {
            if (itemsById.Count == 0)
            {
                Debug.LogWarning("[DedicatedServerItemsManager] No items available");
                return null;
            }

            var allIds = GetAllItemIds();
            int randomIndex = Random.Range(0, allIds.Count);
            return allIds[randomIndex];
        }

        /// <summary>
        /// Get multiple random item IDs.
        /// </summary>
        public List<string> GetRandomItemIds(int count)
        {
            var allIds = GetAllItemIds();
            if (allIds.Count == 0)
                return new List<string>();

            var result = new List<string>();
            for (int i = 0; i < count; i++)
            {
                int randomIndex = Random.Range(0, allIds.Count);
                result.Add(allIds[randomIndex]);
            }
            return result;
        }

        /// <summary>
        /// Check if item exists.
        /// </summary>
        public bool HasItem(string itemId)
        {
            return itemsById.ContainsKey(itemId);
        }

        /// <summary>
        /// Reload metadata from API (for detecting new items without restart).
        ///
        /// In world-driven mode this is effectively a no-op and only
        /// clears the local legacy cache.
        /// </summary>
        public IEnumerator ReloadMetadata()
        {
            DebugLog(
                "ReloadMetadata called on DedicatedServerItemsManager, but legacy metadata "
                    + "loading is disabled. Clearing local cache only."
            );
            itemsById.Clear();
            TotalItemsLoaded = 0;
            yield break;
        }

        void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[DedicatedServerItemsManager] {message}");
            }
        }
    }
}
