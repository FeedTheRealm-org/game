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
    // Lista local de items (no sincronizada directamente)
    private List<ItemData> containedItems = new List<ItemData>();
    
    // Lista temporal para items agregados ANTES del spawn
    private List<ItemData> pendingItems = new List<ItemData>();
    
    // NetworkList para sincronizar IDs de items entre servidor y clientes
    private NetworkList<Unity.Collections.FixedString64Bytes> itemIds;
    
    [Header("Item Database")]
    [SerializeField]
    [Tooltip("Referencia al ItemDatabase para resolver IDs a ItemData")]
    private ItemDatabase itemDatabase;
    
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

    private void Awake() {
        // Inicializar NetworkList
        itemIds = new NetworkList<Unity.Collections.FixedString64Bytes>();
    }

    public override void OnNetworkSpawn() {
        // SI SOMOS SERVIDOR: transferir items pendientes a NetworkList
        if (IsServer && pendingItems.Count > 0) {
            logger?.Log($"[LootItem] OnNetworkSpawn: Transfiriendo {pendingItems.Count} items pendientes a NetworkList", this);
            foreach (var item in pendingItems) {
                if (item != null && !string.IsNullOrEmpty(item.itemId)) {
                    itemIds.Add(new Unity.Collections.FixedString64Bytes(item.itemId));
                }
            }
            pendingItems.Clear(); // Limpiar la lista temporal
        }
        
        // SI SOMOS CLIENTE: suscribirse a cambios y sincronizar
        if (IsClient && !IsServer) {
            itemIds.OnListChanged += OnItemListChanged;
            // Poblar containedItems desde itemIds si ya hay datos
            SyncItemsFromNetwork();
        }
    }

    public override void OnNetworkDespawn() {
        if (itemIds != null) {
            itemIds.OnListChanged -= OnItemListChanged;
        }
    }

    private void OnItemListChanged(NetworkListEvent<Unity.Collections.FixedString64Bytes> changeEvent) {
        // Re-sincronizar items cuando la lista cambia
        SyncItemsFromNetwork();
    }

    private void SyncItemsFromNetwork() {
        if (itemDatabase == null) {
            Debug.LogError("[LootItem] ItemDatabase no asignado! No se pueden resolver los items.", this);
            return;
        }

        containedItems.Clear();
        foreach (var itemId in itemIds) {
            ItemData item = itemDatabase.GetItem(itemId.ToString());
            if (item != null) {
                containedItems.Add(item);
            } else {
                Debug.LogWarning($"[LootItem] Item con ID '{itemId}' no encontrado en ItemDatabase", this);
            }
        }

        logger?.Log($"[LootItem] Sincronizados {containedItems.Count} items desde red", this);
    }

    private void Start() {
        // Inicializar ItemDatabase si no está asignado
        if (itemDatabase == null) {
            // Intentar cargar desde Resources (funciona en builds)
            itemDatabase = Resources.Load<ItemDatabase>("MainItemDatabase");
            
            if (itemDatabase != null) {
                logger?.Log($"[LootItem] ItemDatabase cargado desde Resources: {itemDatabase.name}", this);
                itemDatabase.Initialize();
            } else {
                Debug.LogError("[LootItem] ❌ NO SE ENCONTRÓ ItemDatabase! Asegúrate de:" +
                    "\n1. El archivo se llama EXACTAMENTE 'MainItemDatabase.asset'" +
                    "\n2. Está en la carpeta: Assets/.../Items/Resources/" +
                    "\n3. O asígnalo manualmente al prefab LootItem en el Inspector", this);
            }
        } else {
            // Si ya está asignado, inicializarlo
            itemDatabase.Initialize();
        }
        
        // Ajustar la posición vertical si es necesario
        if (heightOffset != 0) {
            transform.position += Vector3.up * heightOffset;
        }
        
        // Verificar que hay un visual asignado
        if (itemVisual == null) {
            logger?.Log($"[LootItem] Warning: No visual assigned for loot '{itemName}'", this, Logging.LogType.Warning);
        } else {
            // Mostrar qué items contiene la bolsa (filtrando nulls)
            var validItems = new System.Collections.Generic.List<string>();
            foreach (var item in containedItems) {
                if (item != null) {
                    validItems.Add(item.displayName);
                }
            }
            
            string itemNames = validItems.Count > 0 
                ? string.Join(", ", validItems)
                : "ningún item válido";
            
            logger?.Log($"[LootItem] Loot '{itemName}' spawned at {transform.position} con {containedItems.Count} items: [{itemNames}]", this);
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
        // CRÍTICO: En multiplayer, los triggers se ejecutan en TODOS (servidor + clientes)
        // Solo queremos procesar en clientes que tengan UI/Inventarios
        
        // Verificar si es el jugador
        if (((1 << other.gameObject.layer) & playerLayer) != 0) {
            logger?.Log($"[LootItem] Jugador detectado: {other.name}", this);
            
            // En multiplayer, SOLO procesar si NO somos dedicated server
            bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isMultiplayer) {
                // Si somos servidor puro (no host), ignorar
                if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost) {
                    logger?.Log($"[LootItem] Dedicated Server ignorando trigger", this);
                    return;
                }
            }
            
            playersInRange.Add(other);
            
            // Intentar recoger inmediatamente
            TryPickupItems(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other) {
        // Remover jugador del rango y resetear flag
        if (((1 << other.gameObject.layer) & playerLayer) != 0) {
            // El servidor no necesita trackear jugadores en rango
            bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (isMultiplayer && NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost) {
                return;
            }
            
            logger?.Log($"[LootItem] Jugador salió del rango: {other.name}", this);
            playersInRange.Remove(other);
            
            // Si el jugador sale, permitir reintentar cuando vuelva a entrar
            if (playersInRange.Count == 0) {
                hasAttemptedPickup = false;
            }
        }
    }

    /// <summary>
    /// Intenta transferir los items al inventario del jugador
    /// </summary>
    private void TryPickupItems(GameObject player) {
        // Evitar procesamiento múltiple si ya se intentó y falló
        if (hasAttemptedPickup) {
            return;
        }

        // En multiplayer, solo el dueño del jugador puede recoger items para su inventario
        // Esto evita que si ambos clientes detectan la colisión, ambos procesen el pickup
        NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();
        bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        
        if (isMultiplayer && playerNetworkObject != null) {
            // Solo procesar si somos el dueño de este jugador (nuestro cliente local)
            if (!playerNetworkObject.IsOwner) {
                // Esto es normal - otros clientes ven el jugador pero no deben procesar su pickup
                return;
            }
            Debug.Log($"[LootItem] Procesando pickup para nuestro jugador local {player.name}");
        }

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
        if (containedItems.Count == 0) {
            Debug.Log($"[LootItem] La bolsa está vacía (ya fue looteada), destruyendo...");
            DestroyLootBag(player);
            return;
        }
        
        Debug.Log($"[LootItem] Bolsa tiene {containedItems.Count} items, procediendo con transferencia");

        // Contar cuántos slots vacíos hay
        int emptySlots = inventory.GetEmptySlotCount();
        bool isFull = inventory.IsInventoryFull();
        logger?.Log($"[LootItem] Intentando transferir {containedItems.Count} items. Slots vacíos: {emptySlots}, IsFull: {isFull}", this);
        logger?.Log($"[LootItem] InventoryController GameObject: {inventory.gameObject.name}, IsActive: {inventory.gameObject.activeInHierarchy}", this);

        // Si no hay espacio, marcar como intentado y no hacer nada
        if (emptySlots == 0) {
            logger?.Log($"[LootItem] Inventario lleno! No se puede recoger el loot.", this, Logging.LogType.Warning);
            hasAttemptedPickup = true;
            return;
        }

        // Transferir items uno por uno hasta llenar el inventario o vaciar la bolsa
        int itemsTransferred = 0;
        List<ItemData> itemsToRemove = new List<ItemData>();

        foreach (ItemData item in containedItems) {
            if (inventory.IsInventoryFull()) {
                logger?.Log($"[LootItem] Inventario se llenó durante la transferencia. Items transferidos: {itemsTransferred}/{containedItems.Count}", this);
                hasAttemptedPickup = true;
                break;
            }

            // TEMPORAL: Convertir ItemData a Sprite hasta que se actualice InventoryController
            // TODO: Actualizar InventoryController para aceptar ItemData directamente
            if (item != null && item.icon != null) {
                inventory.AddItem(item.icon);
                itemsToRemove.Add(item);
                itemsTransferred++;
            } else {
                Debug.LogWarning($"[LootItem] Item sin sprite válido: {item?.itemId ?? "null"}", this);
            }
        }

        // Remover los items transferidos de la lista
        foreach (ItemData item in itemsToRemove) {
            containedItems.Remove(item);
        }

        logger?.Log($"[LootItem] Transferencia completada: {itemsTransferred} items transferidos. Items restantes en la bolsa: {containedItems.Count}", this);


        // Si se transfirieron todos los items, destruir la bolsa
        if (containedItems.Count == 0) {
            logger?.Log($"[LootItem] Bolsa vacía, procediendo a destruir...", this);
            PlayPickupFeedback();
            DestroyLootBag(player);
        } else {
            // Si quedan items pero el inventario está lleno, marcar como intentado
            logger?.Log($"[LootItem] Inventario lleno, quedan {containedItems.Count} items en la bolsa", this);
            hasAttemptedPickup = true;
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
    }
    
    /// <summary>
    /// Añade items a la bolsa de loot (útil para configurar desde LootDropper)
    /// IMPORTANTE: Llamar ANTES de networkObject.Spawn() en multiplayer
    /// </summary>
    /// <param name="items">Lista de ItemData a añadir</param>
    public void AddItems(List<ItemData> items) {
        if (items == null || items.Count == 0) {
            logger?.Log($"[LootItem] AddItems llamado con lista vacía o null", this, Logging.LogType.Warning);
            return;
        }
        
        bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        int validItemsCount = 0;
        int invalidItemsCount = 0;
        
        foreach (var item in items) {
            if (item != null) {
                // Validar que el item tenga sprite
                if (item.icon != null && !string.IsNullOrEmpty(item.itemId)) {
                    containedItems.Add(item);
                    
                    // Si es multiplayer, agregar a PENDING (no a NetworkList todavía)
                    // Se transferirá en OnNetworkSpawn cuando sea seguro
                    if (isMultiplayer) {
                        pendingItems.Add(item);
                    }
                    
                    validItemsCount++;
                } else {
                    Debug.LogWarning($"[LootItem] ItemData '{item.itemId}' no tiene sprite/ID válido. No se agregará.", this);
                    invalidItemsCount++;
                }
            } else {
                Debug.LogWarning($"[LootItem] Item null encontrado en la lista de loot. Verifica la configuración del LootDropper.", this);
                invalidItemsCount++;
            }
        }
        
        logger?.Log($"[LootItem] {validItemsCount} items válidos añadidos. {invalidItemsCount} items inválidos ignorados. Total en bolsa: {containedItems.Count}", this);
    }

    /// <summary>
    /// Añade un item individual a la bolsa
    /// </summary>
    /// <param name="item">ItemData del item a añadir</param>
    public void AddItem(ItemData item) {
        if (item != null) {
            if (item.icon != null && !string.IsNullOrEmpty(item.itemId)) {
                containedItems.Add(item);
                
                bool isMultiplayer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
                if (isMultiplayer) {
                    pendingItems.Add(item);
                }
                
                logger?.Log($"[LootItem] Item '{item.displayName}' añadido a la bolsa. Total: {containedItems.Count}", this);
            } else {
                Debug.LogWarning($"[LootItem] ItemData '{item.itemId}' no tiene sprite/ID válido. No se agregará.", this);
            }
        } else {
            Debug.LogWarning($"[LootItem] Intento de añadir item null", this);
        }
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
