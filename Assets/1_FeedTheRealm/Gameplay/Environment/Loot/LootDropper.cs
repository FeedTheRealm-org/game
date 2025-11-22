using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Componente que se agrega a los enemigos para que suelten loot al morir.
/// Se suscribe al evento OnDeath del HealthComponent.
/// Compatible con single-player y multiplayer (Netcode).
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
            logger?.Log("[LootDropper] Error: No se encontró HealthComponent en el enemigo!", this, Logging.LogType.Error);
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
    /// Se ejecuta cuando el enemigo muere
    /// </summary>
    private void HandleDeath() {
        // En multiplayer, solo el servidor debe spawnear loot
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) {
            if (!NetworkManager.Singleton.IsServer) {
                logger?.Log("[LootDropper] Cliente ignorando HandleDeath - solo el servidor spawea loot", this);
                return;
            }
        }

        if (alwaysDrop && lootPrefab != null) {
            DropLoot();
        } else if (lootPrefab == null) {
            logger?.Log("[LootDropper] Warning: No hay prefab de loot asignado!", this, Logging.LogType.Warning);
        }
    }

    /// <summary>
    /// Instancia el loot en la posición del enemigo
    /// </summary>
    private void DropLoot() {
        // Calcular posición de spawn con offset aleatorio
        Vector3 spawnPosition = transform.position + spawnOffset;
        
        if (randomOffset > 0) {
            Vector2 randomCircle = Random.insideUnitCircle * randomOffset;
            spawnPosition += new Vector3(randomCircle.x, 0, randomCircle.y);
        }
        
        // Instanciar el loot
        GameObject lootInstance = Instantiate(lootPrefab, spawnPosition, Quaternion.identity);
        
        // Configurar el LootItem ANTES de spawnearlo en red
        LootItem lootItem = lootInstance.GetComponent<LootItem>();
        if (lootItem != null) {
            lootItem.Initialize(spawnPosition);
            
            // Obtener items aleatorios desde el DedicatedServerItemsManager
            List<string> lootItemIds = GetRandomLootItems();
            
            if (lootItemIds != null && lootItemIds.Count > 0) {
                lootItem.SetItemIds(lootItemIds);
                logger?.Log($"[LootDropper] Configured loot with {lootItemIds.Count} item IDs", this);
            } else {
                // Empty loot bag will spawn but won't have items (will be collected but give nothing)
            }
        } else {
            logger?.Log($"[LootDropper] ERROR: Loot prefab no tiene LootItem component!", this, Logging.LogType.Error);
            Destroy(lootInstance);
            return;
        }
        
        // DESPUÉS de configurar, spawnear en la red (si es multiplayer)
        bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (isMultiplayer) {
            NetworkObject networkObject = lootInstance.GetComponent<NetworkObject>();
            if (networkObject != null) {
                networkObject.Spawn();
                logger?.Log($"[LootDropper] Loot spawned as NetworkObject at {spawnPosition}", this);
            } else {
                logger?.Log($"[LootDropper] ERROR: Loot prefab no tiene NetworkObject! El loot no será visible en multiplayer.", this, Logging.LogType.Error);
                logger?.Log($"[LootDropper] Agrega un componente NetworkObject al prefab de loot para multiplayer.", this, Logging.LogType.Error);
                // Destruir el loot local ya que no sirve en multiplayer
                Destroy(lootInstance);
                return;
            }
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
    // Visualización en el editor para ver dónde caerá el loot
    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.cyan;
        Vector3 dropPosition = transform.position + spawnOffset;
        Gizmos.DrawWireSphere(dropPosition, 0.2f);
        
        // Mostrar el área de spawn aleatorio
        if (randomOffset > 0) {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(dropPosition, randomOffset);
        }
    }
#endif
}
