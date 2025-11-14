using UnityEngine;

/// <summary>
/// Componente puente que conecta el jugador con su InventoryController.
/// Debe estar en el GameObject del jugador y referenciar al InventoryController (que puede estar en un prefab de UI separado).
/// Esto permite que otros sistemas (como LootItem) accedan al inventario del jugador de forma multiplayer-friendly.
/// </summary>
public class PlayerInventoryReference : MonoBehaviour {
    
    [Header("Inventory Reference")]
    [SerializeField]
    [Tooltip("Referencia directa al InventoryController. Si no se asigna, se buscará automáticamente en la escena")]
    private InventoryController inventoryController;
    
    [Header("Auto-Configuration")]
    [SerializeField]
    [Tooltip("Si está habilitado, buscará automáticamente el InventoryController en la escena al iniciar")]
    private bool autoFindInventory = true;
    
    [Header("Logging")]
    [SerializeField]
    private Logging.Logger logger;

    private void Awake() {
        InitializeInventoryReference();
    }

    /// <summary>
    /// Inicializa la referencia al inventario. Se puede llamar manualmente si es necesario.
    /// </summary>
    public void InitializeInventoryReference() {
        // Si ya hay una referencia válida y no es un prefab, usarla
        if (inventoryController != null && inventoryController.gameObject.scene.name != null) {
            logger?.Log($"[PlayerInventoryReference] Referencia ya configurada: {inventoryController.gameObject.name}", this);
            ConfigureInventoryController();
            return;
        }

        // Si autoFind está deshabilitado y no hay referencia, advertir
        if (!autoFindInventory && inventoryController == null) {
            logger?.Log("[PlayerInventoryReference] AutoFind deshabilitado y no hay referencia manual. No se configurará inventario.", this, Logging.LogType.Warning);
            return;
        }

        // Búsqueda automática del InventoryController en la escena
        logger?.Log("[PlayerInventoryReference] Buscando InventoryController en la escena...", this);
        inventoryController = FindFirstObjectByType<InventoryController>();
        
        if (inventoryController != null) {
            logger?.Log($"[PlayerInventoryReference] InventoryController encontrado: {inventoryController.gameObject.name}", this);
            ConfigureInventoryController();
        } else {
            logger?.Log("[PlayerInventoryReference] ERROR: No se encontró InventoryController en la escena!", this, Logging.LogType.Error);
        }
    }

    /// <summary>
    /// Configura el InventoryController con el PlayerInputReader del jugador
    /// </summary>
    private void ConfigureInventoryController() {
        if (inventoryController == null) return;

        // PlayerInputReader es un ScriptableObject, no un componente
        // Necesitamos buscarlo en el PlayerController o NetworkPlayerController
        PlayerInputReader playerInputReader = null;

        // Intentar obtener desde PlayerController (single-player)
        var playerController = GetComponent<PlayerController>();
        if (playerController != null) {
            playerInputReader = playerController.inputReader;
        }

        // Si no se encontró, intentar desde NetworkPlayerController (multiplayer)
        if (playerInputReader == null) {
            var networkPlayerController = GetComponent<NetworkPlayerController>();
            if (networkPlayerController != null) {
                // NetworkPlayerController tiene un campo privado serializado, usar reflexión
                var field = networkPlayerController.GetType().GetField("playerInputReader", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field != null) {
                    playerInputReader = field.GetValue(networkPlayerController) as PlayerInputReader;
                }
            }
        }

        if (playerInputReader != null) {
            // Usar reflexión para asignar el playerInputReader al InventoryController
            var field = inventoryController.GetType().GetField("playerInputReader", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (field != null) {
                field.SetValue(inventoryController, playerInputReader);
                logger?.Log($"[PlayerInventoryReference] PlayerInputReader del jugador '{gameObject.name}' asignado al InventoryController", this);
            }
        } else {
            logger?.Log($"[PlayerInventoryReference] WARNING: No se encontró PlayerInputReader en {gameObject.name}. Asegúrate de que PlayerController o NetworkPlayerController esté presente.", this, Logging.LogType.Warning);
        }
    }

    /// <summary>
    /// Obtiene el InventoryController asociado a este jugador
    /// </summary>
    /// <returns>El InventoryController del jugador, o null si no está asignado</returns>
    public InventoryController GetInventory() {
        if (inventoryController == null) {
            logger?.Log("[PlayerInventoryReference] Error: Intentando acceder a un inventario null!", this, Logging.LogType.Error);
        }
        return inventoryController;
    }

    /// <summary>
    /// Establece la referencia al InventoryController (útil para configuración dinámica)
    /// </summary>
    /// <param name="inventory">El InventoryController a asignar</param>
    public void SetInventory(InventoryController inventory) {
        inventoryController = inventory;
        logger?.Log($"[PlayerInventoryReference] Inventory asignado dinámicamente para {gameObject.name}", this);
    }

    /// <summary>
    /// Verifica si hay un inventario válido asignado
    /// </summary>
    /// <returns>True si hay un inventario asignado, false si no</returns>
    public bool HasInventory() {
        return inventoryController != null;
    }
}
