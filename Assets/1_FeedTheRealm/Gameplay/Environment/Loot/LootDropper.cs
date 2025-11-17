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
    
    [Header("Loot Contents")]
    [SerializeField]
    [Tooltip("Tabla de loot que define qué items pueden dropear y sus probabilidades")]
    private LootTable lootTable;
    
    [SerializeField]
    [Tooltip("(LEGACY - Usar LootTable en su lugar) Items que se agregarán directamente sin tabla")]
    private List<ItemData> legacyLootItems = new List<ItemData>();
    
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
        
        // Determinar qué items van a dropear (declarar fuera del scope para poder usar después)
        List<ItemData> itemsToDrop = new List<ItemData>();
        
        // CRÍTICO: Configurar el LootItem ANTES de spawnearlo en red
        LootItem lootItem = lootInstance.GetComponent<LootItem>();
        if (lootItem != null) {
            lootItem.Initialize(spawnPosition);
            
            // Priorizar LootTable si está configurado
            if (lootTable != null) {
                itemsToDrop = lootTable.RollLoot();
                logger?.Log($"[LootDropper] LootTable {lootTable.tableName} rolled: {itemsToDrop.Count} items", this);
            } 
            // Fallback a legacy items si no hay tabla
            else if (legacyLootItems != null && legacyLootItems.Count > 0) {
                itemsToDrop = legacyLootItems;
                logger?.Log($"[LootDropper] Using legacy items: {itemsToDrop.Count}", this);
            } 
            else {
                logger?.Log($"[LootDropper] Warning: No LootTable ni legacy items configurados", this, Logging.LogType.Warning);
            }
            
            // Añadir los items a la bolsa ANTES del Spawn
            if (itemsToDrop.Count > 0) {
                lootItem.AddItems(itemsToDrop);
                logger?.Log($"[LootDropper] Items añadidos al loot: {itemsToDrop.Count}", this);
            } else {
                logger?.Log($"[LootDropper] Warning: No hay items para dropear (roll falló o tabla vacía)", this, Logging.LogType.Warning);
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
                logger?.Log($"[LootDropper] Loot spawned as NetworkObject at {spawnPosition} with {itemsToDrop.Count} items", this);
            } else {
                logger?.Log($"[LootDropper] ERROR: Loot prefab no tiene NetworkObject! El loot no será visible en multiplayer.", this, Logging.LogType.Error);
                logger?.Log($"[LootDropper] Agrega un componente NetworkObject al prefab de loot para multiplayer.", this, Logging.LogType.Error);
                // Destruir el loot local ya que no sirve en multiplayer
                Destroy(lootInstance);
                return;
            }
        }
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
