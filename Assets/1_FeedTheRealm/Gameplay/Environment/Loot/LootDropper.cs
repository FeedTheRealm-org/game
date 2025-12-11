using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Component added to enemies to drop loot upon death.
/// Subscribes to the OnDeath event of the HealthComponent.
/// Compatible with single-player and multiplayer (Netcode).
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class LootDropper : MonoBehaviour {
    
    [Header("Loot configuration")]
    [SerializeField]
    [Tooltip("Prefab of the LootItem to instantiate")]
    private GameObject lootPrefab;
    
    [SerializeField]
    [Tooltip("If enabled, loot will always drop. Otherwise, probability can be used later.")]
    private bool alwaysDrop = true;
    
    [SerializeField]
    [Tooltip("Spawn offset of the loot relative to the enemy's position")]
    private Vector3 spawnOffset = Vector3.zero;
    
    [SerializeField]
    [Tooltip("Adds a random variation to the spawn position")]
    private float randomOffset = 0.5f;
    
    [Header("Loot Configuration")]
    [SerializeField]
    [Tooltip("Number of items to drop (random from all items)")]
    private int itemCount = 1;
    
    [SerializeField]
    [Tooltip("If set, only drop items from this category. Leave empty for any category.")]
    private string categoryFilter = "";
    
    [SerializeField]
    private Logging.Logger logger;

    private HealthComponent healthComponent;

    private void Awake() {
        healthComponent = GetComponent<HealthComponent>();
        
        if (healthComponent == null) {
            logger?.Log("[LootDropper] Error: HealthComponent not found on the enemy!", this, Logging.LogType.Error);
            enabled = false;
            return;
        }
    }

    private void OnEnable() {
        if (healthComponent != null) {
            healthComponent.OnDeath += HandleDeath;
        }
    }

    private void OnDisable() {
        if (healthComponent != null) {
            healthComponent.OnDeath -= HandleDeath;
        }
    }

    /// <summary>
    /// Executes when the enemy dies
    /// </summary>
    private void HandleDeath() {
        // In multiplayer, only the server should spawn loot
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) {
            if (!NetworkManager.Singleton.IsServer) {
                logger?.Log("[LootDropper] Client ignoring HandleDeath - only the server spawns loot", this);
                return;
            }
        }

        if (alwaysDrop && lootPrefab != null) {
            DropLoot();
        } else if (lootPrefab == null) {
            logger?.Log("[LootDropper] Warning: No loot prefab assigned!", this, Logging.LogType.Warning);
        }
    }

    /// <summary>
    /// Instantiates the loot at the enemy's position
    /// </summary>
    private void DropLoot() {
        // Calculate spawn position with random offset
        Vector3 spawnPosition = transform.position + spawnOffset;
        
        if (randomOffset > 0) {
            Vector2 randomCircle = Random.insideUnitCircle * randomOffset;
            spawnPosition += new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        
        // Instantiate the loot
        GameObject lootInstance = Instantiate(lootPrefab, spawnPosition, Quaternion.identity);
        
        // Check components
        LootItem lootItem = lootInstance.GetComponent<LootItem>();
        if (lootItem == null) {
            logger?.Log($"[LootDropper] ERROR: Loot prefab does not have LootItem component!", this, Logging.LogType.Error);
            Destroy(lootInstance);
            return;
        }

        // Initialize position (does not touch NetworkVariables)
        lootItem.Initialize(spawnPosition);
        
        // CONFIGURE THE ITEMS BEFORE SPAWNING ON THE NETWORK
        List<string> lootItemIds = GetRandomLootItems();
        
        if (lootItemIds != null && lootItemIds.Count > 0) {
            lootItem.SetItemIds(lootItemIds);
            logger?.Log($"[LootDropper] Configured loot with {lootItemIds.Count} item IDs: {string.Join(", ", lootItemIds)}", this);
        } else {
            logger?.Log("[LootDropper] WARNING: No items obtained for loot bag - loot will spawn empty!", this, Logging.LogType.Warning);
        }
        
        // NOW spawn on the network (after configuring items)
        bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isMultiplayer) {
            NetworkObject networkObject = lootInstance.GetComponent<NetworkObject>();
            if (networkObject == null) {
                logger?.Log($"[LootDropper] ERROR: Loot prefab does not have NetworkObject! The loot will not be visible in multiplayer.", this, Logging.LogType.Error);
                Destroy(lootInstance);
                return;
            }

            // Spawn on the network AFTER configuring items
            networkObject.Spawn();
            logger?.Log($"[LootDropper] Loot spawned as NetworkObject at {spawnPosition} with {lootItemIds?.Count ?? 0} items", this);
        }
    }

    /// <summary>
    /// Get random item IDs from the server's items manager.
    /// Uses category filter if configured.
    /// </summary>
    private List<string> GetRandomLootItems() {
        // Try DedicatedServerItemsManager first (for dedicated server builds)
        if (Items.DedicatedServerItemsManager.Instance != null) {
            if (!Items.DedicatedServerItemsManager.Instance.IsInitialized) {
                logger?.Log("[LootDropper] WARNING: DedicatedServerItemsManager not initialized yet! Items will not drop until initialization completes.", this, Logging.LogType.Warning);
                logger?.Log("[LootDropper] This is normal on first enemy death. Subsequent deaths should work fine.", this, Logging.LogType.Warning);
                return new List<string>();
            }

            return GetRandomItemsFromServerManager();
        }
        
        // Fallback: Try ItemsManager (for client/host)
        if (Items.ItemsManager.Instance != null) {
            if (!Items.ItemsManager.Instance.IsInitialized) {
                logger?.Log("[LootDropper] WARNING: ItemsManager not initialized yet! Items will not drop.", this, Logging.LogType.Warning);
                return new List<string>();
            }

            return GetRandomItemsFromClientManager();
        }

        logger?.Log("[LootDropper] ERROR: No ItemsManager found (neither Server nor Client)!", this, Logging.LogType.Error);
        return new List<string>();
    }

    private List<string> GetRandomItemsFromServerManager() {
        var result = new List<string>();

        for (int i = 0; i < itemCount; i++) {
            string itemId;
            
            if (!string.IsNullOrEmpty(categoryFilter)) {
                itemId = Items.DedicatedServerItemsManager.Instance.GetRandomItemIdFromCategory(categoryFilter);
            } else {
                itemId = Items.DedicatedServerItemsManager.Instance.GetRandomItemId();
            }

            if (!string.IsNullOrEmpty(itemId)) {
                result.Add(itemId);
            }
        }

        return result;
    }

    private List<string> GetRandomItemsFromClientManager() {
        var result = new List<string>();

        for (int i = 0; i < itemCount; i++) {
            string itemId;
            
            var allItems = Items.ItemsManager.Instance.GetAllItems();
            if (allItems.Length == 0) continue;

            if (!string.IsNullOrEmpty(categoryFilter)) {
                var categoryItems = Items.ItemsManager.Instance.GetItemsByCategory(categoryFilter);
                if (categoryItems.Count > 0) {
                    int randomIndex = UnityEngine.Random.Range(0, categoryItems.Count);
                    itemId = categoryItems[randomIndex].id;
                    result.Add(itemId);
                }
            } else {
                int randomIndex = UnityEngine.Random.Range(0, allItems.Length);
                itemId = allItems[randomIndex].id;
                result.Add(itemId);
            }
        }

        return result;
    }

#if UNITY_EDITOR
    // Visualization in the editor to see where the loot will drop
    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.cyan;
        Vector3 dropPosition = transform.position + spawnOffset;
        Gizmos.DrawWireSphere(dropPosition, 0.2f);
        
        // Show the random spawn area
        if (randomOffset > 0) {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(dropPosition, randomOffset);
        }
    }
#endif
}
