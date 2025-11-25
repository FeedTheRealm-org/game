using UnityEngine;

/// <summary>
/// Componente puente que conecta el jugador con su InventoryController.
/// Debe estar en el GameObject del jugador y referenciar al InventoryController (que puede estar en un prefab de UI separado).
/// Esto permite que otros sistemas (como LootItem) accedan al inventario del jugador de forma multiplayer-friendly.
/// </summary>
public class PlayerInventoryReference : MonoBehaviour {
    
    [Header("Inventory Reference")]
    [SerializeField]
    [Tooltip("Prefab del InventoryController que se instanciará para este jugador")]
    private GameObject inventoryPrefab;
    
    [SerializeField]
    [Tooltip("Referencia al InventoryController instanciado")]
    private InventoryController inventoryController;
    
    [Header("Auto-Configuration")]
    [SerializeField]
    [Tooltip("Si está habilitado, buscará el InventoryController en GameSceneManager si es el jugador local")]
    private bool useSceneInventoryForLocalPlayer = true;
    
    [Header("Logging")]
    [SerializeField]
    private Logging.Logger logger;
    
    private bool isLocalPlayer = false;
    private bool isInitialized = false;

    private void Start() {
        // Inicializar en Start en lugar de Awake para dar tiempo a que NetworkBehaviour se configure
        // También intentar inicializar aquí como fallback para single-player
        if (!isInitialized) {
            InitializeInventoryReference();
        }
    }
    
    /// <summary>
    /// Llamar este método desde NetworkPlayerController.OnNetworkSpawn() para garantizar que IsOwner esté correctamente establecido
    /// </summary>
    public void InitializeForNetworkedPlayer() {
        if (!isInitialized) {
            logger?.Log("[PlayerInventoryReference] InitializeForNetworkedPlayer llamado desde NetworkPlayerController", this);
            InitializeInventoryReference();
        }
    }

    /// <summary>
    /// Inicializa la referencia al inventario. Se puede llamar manualmente si es necesario.
    /// </summary>
    public void InitializeInventoryReference() {
        if (isInitialized) {
            logger?.Log("[PlayerInventoryReference] Ya inicializado, ignorando llamada duplicada", this);
            return;
        }
        
        // Detectar si este es el jugador local
        DetectLocalPlayer();
        
        // Si ya hay una referencia válida y no es un prefab, usarla
        if (inventoryController != null && inventoryController.gameObject.scene.name != null) {
            logger?.Log($"[PlayerInventoryReference] Referencia ya configurada: {inventoryController.gameObject.name}", this);
            ConfigureInventoryController();
            isInitialized = true;
            return;
        }

        // Si es el jugador local, buscar el inventario del GameSceneManager
        if (isLocalPlayer && useSceneInventoryForLocalPlayer) {
            logger?.Log("[PlayerInventoryReference] Jugador local detectado. Buscando InventoryController en GameSceneManager...", this);
            
            var gameSceneManager = FindFirstObjectByType<GameSceneManager>();
            if (gameSceneManager != null) {
                // Usar reflexión para acceder al campo privado inventoryMenu
                var field = gameSceneManager.GetType().GetField("inventoryMenu", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field != null) {
                    inventoryController = field.GetValue(gameSceneManager) as InventoryController;
                    
                    if (inventoryController != null) {
                        logger?.Log($"[PlayerInventoryReference] InventoryController obtenido de GameSceneManager: {inventoryController.gameObject.name}", this);
                        ConfigureInventoryController();
                        isInitialized = true;
                        return;
                    }
                }
            }
            
            logger?.Log("[PlayerInventoryReference] No se pudo obtener InventoryController de GameSceneManager", this, Logging.LogType.Warning);
        }

        // Si no es jugador local o no se encontró en GameSceneManager, instanciar uno nuevo
        if (inventoryPrefab != null) {
            logger?.Log("[PlayerInventoryReference] Instanciando InventoryController desde prefab...", this);
            
            GameObject inventoryGO = Instantiate(inventoryPrefab);
            inventoryGO.name = $"Inventory_{gameObject.name}";
            inventoryController = inventoryGO.GetComponent<InventoryController>();
            
            if (inventoryController != null) {
                logger?.Log($"[PlayerInventoryReference] InventoryController instanciado: {inventoryGO.name}", this);
                
                // Si no es el jugador local, ocultar la UI del inventario y deshabilitar el componente Start()
                if (!isLocalPlayer) {
                    var uiDocument = inventoryGO.GetComponent<UnityEngine.UIElements.UIDocument>();
                    if (uiDocument != null) {
                        uiDocument.enabled = false;
                        logger?.Log($"[PlayerInventoryReference] UI del inventario deshabilitada para jugador remoto", this);
                    }
                    
                    // Deshabilitar el InventoryController para jugadores remotos (evita errores en Start())
                    inventoryController.enabled = false;
                    logger?.Log($"[PlayerInventoryReference] InventoryController deshabilitado para jugador remoto (solo almacenamiento de datos)", this);
                }
                
                ConfigureInventoryController();
                isInitialized = true;
            } else {
                logger?.Log("[PlayerInventoryReference] ERROR: El prefab no contiene un InventoryController!", this, Logging.LogType.Error);
            }
        } else {
            logger?.Log("[PlayerInventoryReference] ERROR: No hay prefab de inventario asignado y no se pudo obtener uno de la escena!", this, Logging.LogType.Error);
        }
        
        isInitialized = true;
    }
    
    /// <summary>
    /// Detecta si este es el jugador local (el que controla el cliente actual)
    /// </summary>
    private void DetectLocalPlayer() {
        // Intentar detectar desde Unity.Netcode.NetworkBehaviour
        var networkBehaviour = GetComponent<Unity.Netcode.NetworkBehaviour>();
        if (networkBehaviour != null) {
            isLocalPlayer = networkBehaviour.IsOwner;
            logger?.Log($"[PlayerInventoryReference] Jugador {(isLocalPlayer ? "LOCAL" : "REMOTO")} detectado (NetworkBehaviour)", this);
            return;
        }
        
        // Fallback: Si no hay NetworkBehaviour, asumir que es local (modo single-player)
        isLocalPlayer = true;
        logger?.Log($"[PlayerInventoryReference] No se encontró NetworkBehaviour. Asumiendo jugador LOCAL", this);
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
