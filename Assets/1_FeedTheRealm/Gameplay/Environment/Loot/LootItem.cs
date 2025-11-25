using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Representa un ítem de loot (bolsa) que aparece en el mundo cuando un enemigo muere.
/// Al acercarse, automáticamente transfiere los items al inventario del jugador.
/// Compatible con single-player y multiplayer (Netcode).
/// </summary>
public class LootItem : NetworkBehaviour {
    
    [Header("Loot Configuration")]
    [SerializeField]
    [Tooltip("The sprite or visual model that will be shown on the ground")]
    private GameObject itemVisual;
    
    [SerializeField]
    [Tooltip("Item name (optional, for debug)")]
    private string itemName = "Loot Bag";
    
    [SerializeField]
    [Tooltip("Vertical offset from the spawn point")]
    private float heightOffset = 0.1f;
    
    [Header("Loot Contents")]
    // NetworkList para sincronizar IDs de items entre servidor y clientes
    private NetworkList<Unity.Collections.FixedString64Bytes> itemIds;
    
    [Header("Pickup Settings")]
    [SerializeField]
    [Tooltip("Pickup radius (trigger zone size)")]
    private float pickupRadius = 1.5f;
    
    [SerializeField]
    [Tooltip("Layer mask for detecting the player")]
    private LayerMask playerLayer;
    
    [Header("Feedback (Optional)")]
    [SerializeField]
    [Tooltip("Audio clip to play when items are picked up (optional)")]
    private AudioClip pickupSound;
    
    [SerializeField]
    [Tooltip("Particle effect to spawn when picked up (optional)")]
    private GameObject pickupVFX;
    
    [SerializeField]
    private Logging.Logger logger;

    private SphereCollider triggerCollider;
    private bool hasAttemptedPickup = false;
    private HashSet<Collider> playersInRange = new HashSet<Collider>();
    
    // Delay para evitar loot inmediato después del spawn
    private float spawnTime;
    private bool isLootable = false;
    [SerializeField]
    [Tooltip("Delay in seconds before loot becomes collectible")]
    private float lootableDelay = 1.0f;

    private void Awake() {
        // Inicializar NetworkList
        itemIds = new NetworkList<Unity.Collections.FixedString64Bytes>();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        
        logger?.Log($"[LootItem] OnNetworkSpawn - IsServer: {IsServer}, ItemCount: {itemIds.Count}, Items: {string.Join(", ", itemIds)}", this);
        
        // Clientes: Esperar y actualizar visuals
        if (IsClient) {
            StartCoroutine(WaitForItemsManagerAndUpdateVisuals());
        }
    }

    public override void OnNetworkDespawn() {
        if (itemIds != null) {
            itemIds.OnListChanged -= OnItemListChanged;
        }
    }

    private void OnItemListChanged(NetworkListEvent<Unity.Collections.FixedString64Bytes> changeEvent) {
        // Re-sincronizar visuals cuando la lista cambia
        if (IsClient) {
            UpdateVisualsFromManager();
        }
    }

    private System.Collections.IEnumerator WaitForItemsManagerAndUpdateVisuals() {
        // Esperar a que ItemsManager esté inicializado
        while (Items.ItemsManager.Instance == null || !Items.ItemsManager.Instance.IsInitialized) {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Suscribirse a cambios
        itemIds.OnListChanged += OnItemListChanged;
        
        // Actualizar visuals
        UpdateVisualsFromManager();
    }

    private string GetFirstItemId() {
        if (itemIds != null && itemIds.Count > 0) {
            return itemIds[0].ToString();
        }
        return "";
    }

    private void UpdateVisualsFromManager() {
        if (Items.ItemsManager.Instance == null || !Items.ItemsManager.Instance.IsInitialized) {
            Debug.LogWarning("[LootItem] ItemsManager not available or not initialized");
            return;
        }

        logger?.Log($"[LootItem] Updating visuals for {itemIds.Count} items", this);
        
        // Aquí podrías actualizar el visual del loot bag basado en los items
        // Por ahora solo logueamos
        foreach (var itemId in itemIds) {
            string id = itemId.ToString();
            var metadata = Items.ItemsManager.Instance.GetItemById(id);
            if (metadata != null) {
                logger?.Log($"[LootItem] Contains: {metadata.name}", this);
            }
        }
    }

    private void Start() {
        // Ajustar la posición vertical si es necesario
        if (heightOffset != 0) {
            transform.position += Vector3.up * heightOffset;
        }
        
        // Verificar que hay un visual asignado
        if (itemVisual == null) {
            logger?.Log($"[LootItem] Warning: No visual assigned for loot '{itemName}'", this, Logging.LogType.Warning);
        } else {
            logger?.Log($"[LootItem] Loot '{itemName}' spawned at {transform.position} with {itemIds.Count} item IDs", this);
        }
        
        // Crear el trigger collider para detección de jugador
        SetupTriggerCollider();
    }

    /// <summary>
    /// Configura el trigger collider como hijo para detectar jugadores
    /// </summary>
    private void SetupTriggerCollider() {
        // Crear un GameObject hijo para el trigger
        GameObject triggerObj = new GameObject("PickupTrigger");
        triggerObj.transform.SetParent(transform);
        triggerObj.transform.localPosition = Vector3.zero;
        triggerObj.layer = gameObject.layer;
        
        // Añadir y configurar el SphereCollider como trigger
        triggerCollider = triggerObj.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = pickupRadius;
        
        logger?.Log($"[LootItem] Trigger collider configurado con radio {pickupRadius}", this);
    }

    private void OnTriggerEnter(Collider other) {
        // Verificar si es el jugador
        if (((1 << other.gameObject.layer) & playerLayer) != 0) {
            //logger?.Log($"[LootItem] Jugador detectado: {other.name}", this);
            
            playersInRange.Add(other);
            
            // Solo intentar recoger si el loot ya es looteable
            if (isLootable) {
                TryPickupItems(other.gameObject);
            } else {
                float timeSinceSpawn = Time.time - spawnTime;
                //logger?.Log($"[LootItem] Loot no looteable aún. Tiempo transcurrido: {timeSinceSpawn:F2}s de {lootableDelay}s", this);
            }
        }
    }

    private void Update() {
        // Si hay jugadores en rango pero el loot no es looteable aún, verificar periódicamente
        if (!isLootable && playersInRange.Count > 0) {
            if (Time.time - spawnTime >= lootableDelay) {
                isLootable = true;
                //logger?.Log($"[LootItem] Loot se volvió looteable mientras había jugadores en rango", this);
                
                // Intentar recoger para todos los jugadores en rango
                foreach (var playerCollider in playersInRange) {
                    if (playerCollider != null && playerCollider.gameObject != null) {
                        TryPickupItems(playerCollider.gameObject);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Intenta transferir los items al inventario del jugador
    /// </summary>
    private void TryPickupItems(GameObject player) {
        //logger?.Log($"[LootItem] TryPickupItems called - IsLootable: {isLootable}, ItemCount: {itemIds.Count}, TimeSinceSpawn: {Time.time - spawnTime:F2}s", this);
        
        // Evitar procesamiento múltiple si ya se intentó y falló
        if (hasAttemptedPickup) {
            //logger?.Log("[LootItem] TryPickupItems - Already attempted pickup, skipping", this);
            return;
        }

        // SEPARACIÓN SERVER/CLIENT: Diferentes responsabilidades
        bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();
        
        if (isMultiplayer && IsServer) {
            // ============================================
            // SERVIDOR: Maneja NetworkList y despawn
            // ============================================
            Debug.Log($"[LootItem] SERVER - Procesando pickup. Items en bolsa: {itemIds.Count}");
            
            // Verificar si hay items para transferir
            if (itemIds.Count == 0) {
                Debug.Log($"[LootItem] SERVER - Bolsa vacía, haciendo despawn");
                if (NetworkObject.IsSpawned) {
                    NetworkObject.Despawn(true);
                }
                return;
            }
            
            // Remover todos los items del NetworkList (servidor tiene autoridad)
            int itemsRemoved = itemIds.Count;
            itemIds.Clear();
            
            Debug.Log($"[LootItem] SERVER - Removidos {itemsRemoved} items del NetworkList. Haciendo despawn...");
            
            // Despawn del objeto de red
            if (NetworkObject.IsSpawned) {
                NetworkObject.Despawn(true);
            }
            
        } else {
            // ============================================
            // CLIENTE: Maneja inventario UI y feedback
            // ============================================
            
            // Solo procesar si somos el dueño de este jugador (nuestro cliente local)
            if (isMultiplayer && playerNetworkObject != null && !playerNetworkObject.IsOwner) {
                return;
            }
            
            Debug.Log($"[LootItem] CLIENT - Procesando pickup para jugador local {player.name}");

            // Buscar el PlayerInventoryReference en el jugador
            PlayerInventoryReference inventoryRef = player.GetComponent<PlayerInventoryReference>();
            
            if (inventoryRef == null) {
                logger?.Log($"[LootItem] No se encontró PlayerInventoryReference en {player.name}", this, Logging.LogType.Warning);
                hasAttemptedPickup = true;
                return;
            }

            // Obtener el InventoryController desde la referencia
            InventoryController inventory = inventoryRef.GetInventory();
            
            if (inventory == null) {
                logger?.Log($"[LootItem] PlayerInventoryReference no tiene InventoryController asignado en {player.name}", this, Logging.LogType.Warning);
                hasAttemptedPickup = true;
                return;
            }

            // Verificar si hay items para transferir
            if (itemIds.Count == 0) {
                logger?.Log($"[LootItem] CLIENT - Bolsa vacía (sincronizada desde servidor)", this);
                hasAttemptedPickup = true;
                return;
            }
            
            logger?.Log($"[LootItem] CLIENT - Bolsa tiene {itemIds.Count} items, procediendo con transferencia a UI", this);

            // Contar cuántos slots vacíos hay
            int emptySlots = inventory.GetEmptySlotCount();
            logger?.Log($"[LootItem] CLIENT - Slots vacíos: {emptySlots}, Inventario lleno: {inventory.IsInventoryFull()}", this);

            // Si no hay espacio, marcar como intentado y no hacer nada
            if (emptySlots == 0) {
                logger?.Log($"[LootItem] CLIENT - Inventario lleno! No se puede recoger el loot.", this, Logging.LogType.Warning);
                hasAttemptedPickup = true;
                return;
            }

            // Transferir items uno por uno hasta llenar el inventario o vaciar la bolsa
            int itemsTransferred = 0;

            // Convertir NetworkList a lista temporal para iterar
            List<string> itemIdsList = new List<string>();
            for (int i = 0; i < itemIds.Count; i++) {
                itemIdsList.Add(itemIds[i].ToString());
            }

            foreach (string itemId in itemIdsList) {
                if (inventory.IsInventoryFull()) {
                    logger?.Log($"[LootItem] CLIENT - Inventario lleno. Items transferidos: {itemsTransferred}/{itemIds.Count}", this);
                    hasAttemptedPickup = true;
                    break;
                }

                // Transfer item by ID to inventory UI
                // InventoryController will handle sprite loading from ItemsManager
                inventory.AddItemById(itemId);
                itemsTransferred++;
                
                logger?.Log($"[LootItem] CLIENT - Item transferido a UI: {itemId}", this);
            }

            logger?.Log($"[LootItem] CLIENT - Transferencia completada: {itemsTransferred} items a inventario", this);

            // Reproducir feedback visual/sonoro
            if (itemsTransferred > 0) {
                PlayPickupFeedback();
            }

            // Si el inventario se llenó antes de vaciar la bolsa, marcar como intentado
            if (itemsTransferred < itemIdsList.Count) {
                logger?.Log($"[LootItem] CLIENT - Inventario lleno, no se pudieron transferir todos los items", this);
                hasAttemptedPickup = true;
            }
        }
    }

    /// <summary>
    /// Reproduce efectos de sonido y visuales al recoger
    /// </summary>
    private void PlayPickupFeedback() {
        // Reproducir sonido si está asignado
        if (pickupSound != null) {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            logger?.Log($"[LootItem] Reproduciendo sonido de pickup", this);
        }

        // Instanciar VFX si está asignado
        if (pickupVFX != null) {
            Instantiate(pickupVFX, transform.position, Quaternion.identity);
            logger?.Log($"[LootItem] Spawneando VFX de pickup", this);
        }
    }

    /// <summary>
    /// Destruye la bolsa de loot (compatible con multiplayer)
    /// </summary>
    private void DestroyLootBag(GameObject player) {
        // En multiplayer, solo el servidor puede destruir NetworkObjects
        NetworkObject networkObject = GetComponent<NetworkObject>();
        bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        
        if (isMultiplayer && networkObject != null && networkObject.IsSpawned) {
            logger?.Log($"[LootItem] DestroyLootBag - IsServer={NetworkManager.Singleton.IsServer}, IsHost={NetworkManager.Singleton.IsHost}, IsClient={NetworkManager.Singleton.IsClient}", this);
            
            // CRÍTICO: En multiplayer, usar el ServerRpc del NetworkPlayerController
            // Esto evita problemas de ownership y contexto de servidor
            NetworkPlayerController playerController = player.GetComponent<NetworkPlayerController>();
            
            if (playerController != null) {
                logger?.Log($"[LootItem] Solicitando despawn via NetworkPlayerController NetworkObjectId={networkObject.NetworkObjectId}", this);
                playerController.RequestDespawnLootServerRpc(networkObject.NetworkObjectId);
            } else {
                logger?.Log($"[LootItem] Player no tiene NetworkPlayerController! Usando fallback", this);
                
                // Fallback: Si somos dedicated server, despawnear directo
                if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient) {
                    logger?.Log($"[LootItem] DEDICATED SERVER despawneando directamente NetworkObjectId={networkObject.NetworkObjectId}", this);
                    networkObject.Despawn(true);
                } else {
                    logger?.Log($"[LootItem] No se pudo despawnear el loot", this);
                }
            }
        } else if (!isMultiplayer) {
            // Single-player - usar Destroy normal
            logger?.Log($"[LootItem] Single-player: destruyendo con Destroy()", this);
            Destroy(gameObject);
        } else {
            // Caso edge: multiplayer pero sin NetworkObject o ya despawned
            logger?.Log($"[LootItem] Intento de destruir loot sin NetworkObject válido. IsSpawned={networkObject?.IsSpawned}", this);
        }
    }

    /// <summary>
    /// Inicializa el loot item en una posición específica
    /// </summary>
    /// <param name="spawnPosition">Posición donde aparecerá el loot</param>
    public void Initialize(Vector3 spawnPosition) {
        transform.position = spawnPosition + Vector3.up * heightOffset;
        
        // Registrar tiempo de spawn y iniciar delay
        spawnTime = Time.time;
        isLootable = false;
        
        // Iniciar coroutine para habilitar loot después del delay
        StartCoroutine(EnableLootAfterDelay());
        
        //logger?.Log($"[LootItem] Inicializado en {spawnPosition}. Loot será looteable en {lootableDelay}s", this);
    }
    
    /// <summary>
    /// Coroutine que espera el delay antes de permitir que el loot sea recolectado
    /// </summary>
    private System.Collections.IEnumerator EnableLootAfterDelay() {
        yield return new WaitForSeconds(lootableDelay);
        
        isLootable = true;
        //logger?.Log($"[LootItem] Loot ahora es looteable después de {lootableDelay}s", this);
    }
    
    /// <summary>
    /// Configura los item IDs para esta bolsa de loot.
    /// IMPORTANTE: Llamar ANTES de networkObject.Spawn() en multiplayer.
    /// Solo el servidor debe llamar este método.
    /// </summary>
    /// <param name="ids">Lista de item IDs a configurar</param>
    public void SetItemIds(List<string> ids) {
        if (!NetworkManager.Singleton.IsServer) {
            Debug.LogWarning("[LootItem] SetItemIds should only be called by server!");
            return;
        }

        //logger?.Log($"[LootItem] SetItemIds called with {ids?.Count ?? 0} items: {string.Join(", ", ids ?? new List<string>())}", this);

        if (ids == null || ids.Count == 0) {
            logger?.Log("[LootItem] SetItemIds called with empty or null list", this, Logging.LogType.Warning);
            return;
        }
        
        foreach (var id in ids) {
            if (!string.IsNullOrEmpty(id)) {
                itemIds.Add(new Unity.Collections.FixedString64Bytes(id));
            }
        }
        
        logger?.Log($"[LootItem] Configured {itemIds.Count} item IDs", this);
    }
    
    /// <summary>
    /// Cambia el visual del loot en runtime (útil para diferentes tipos de loot)
    /// </summary>
    /// <param name="newVisual">El nuevo GameObject visual</param>
    public void SetVisual(GameObject newVisual) {
        // Destruir el visual anterior si existe
        if (itemVisual != null) {
            Destroy(itemVisual);
        }
        
        itemVisual = Instantiate(newVisual, transform);
        itemVisual.transform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// Establece el nombre del ítem (útil para debug y futuras features)
    /// </summary>
    /// <param name="name">Nombre del ítem</param>
    public void SetItemName(string name) {
        itemName = name;
        gameObject.name = $"Loot_{name}";
    }

#if UNITY_EDITOR
    // Visualización en el editor para facilitar el debug
    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        // Dibujar el radio de pickup
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
#endif
}
