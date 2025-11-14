using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Representa un ítem de loot (bolsa) que aparece en el mundo cuando un enemigo muere.
/// Al acercarse, automáticamente transfiere los items al inventario del jugador.
/// </summary>
public class LootItem : MonoBehaviour {
    
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
    [SerializeField]
    [Tooltip("List of item sprites that this loot bag contains")]
    private List<Sprite> containedItems = new List<Sprite>();
    
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

    private void Start() {
        // Ajustar la posición vertical si es necesario
        if (heightOffset != 0) {
            transform.position += Vector3.up * heightOffset;
        }
        
        // Verificar que hay un visual asignado
        if (itemVisual == null) {
            logger?.Log($"[LootItem] Warning: No visual assigned for loot '{itemName}'", this, Logging.LogType.Warning);
        } else {
            logger?.Log($"[LootItem] Loot '{itemName}' spawned at {transform.position} with {containedItems.Count} items", this);
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
            logger?.Log($"[LootItem] Jugador detectado: {other.name}", this);
            playersInRange.Add(other);
            
            // Intentar recoger inmediatamente
            TryPickupItems(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other) {
        // Remover jugador del rango y resetear flag
        if (((1 << other.gameObject.layer) & playerLayer) != 0) {
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
            logger?.Log($"[LootItem] La bolsa está vacía, destruyendo...", this);
            DestroyLootBag();
            return;
        }

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
        List<Sprite> itemsToRemove = new List<Sprite>();

        foreach (Sprite item in containedItems) {
            if (inventory.IsInventoryFull()) {
                logger?.Log($"[LootItem] Inventario se llenó durante la transferencia. Items transferidos: {itemsTransferred}/{containedItems.Count}", this);
                hasAttemptedPickup = true;
                break;
            }

            inventory.AddItem(item);
            itemsToRemove.Add(item);
            itemsTransferred++;
        }

        // Remover los items transferidos de la lista
        foreach (Sprite item in itemsToRemove) {
            containedItems.Remove(item);
        }

        logger?.Log($"[LootItem] Transferencia completada: {itemsTransferred} items transferidos", this);

        // Si se transfirieron todos los items, destruir la bolsa
        if (containedItems.Count == 0) {
            PlayPickupFeedback();
            DestroyLootBag();
        } else {
            // Si quedan items pero el inventario está lleno, marcar como intentado
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
    /// Destruye la bolsa de loot
    /// </summary>
    private void DestroyLootBag() {
        logger?.Log($"[LootItem] Destruyendo bolsa de loot", this);
        Destroy(gameObject);
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
    /// </summary>
    /// <param name="items">Lista de sprites a añadir</param>
    public void AddItems(List<Sprite> items) {
        if (items != null && items.Count > 0) {
            containedItems.AddRange(items);
            logger?.Log($"[LootItem] {items.Count} items añadidos a la bolsa. Total: {containedItems.Count}", this);
        }
    }

    /// <summary>
    /// Añade un item individual a la bolsa
    /// </summary>
    /// <param name="item">Sprite del item a añadir</param>
    public void AddItem(Sprite item) {
        if (item != null) {
            containedItems.Add(item);
            logger?.Log($"[LootItem] Item añadido a la bolsa. Total: {containedItems.Count}", this);
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
