using UnityEngine;

/// <summary>
/// Bridge component that connects the player with their InventoryController.
/// Must be on the player's GameObject and reference the InventoryController (which can be in a separate UI prefab).
/// This allows other systems (like LootItem) to access the player's inventory in a multiplayer-friendly way.
/// </summary>
public class PlayerInventoryReference : MonoBehaviour {

    [Header("Inventory Reference")]
    [SerializeField]
    [Tooltip("InventoryController prefab that will be instantiated for this player")]
    private GameObject inventoryPrefab;

    [SerializeField]
    [Tooltip("Reference to the instantiated InventoryController")]
    private InventoryController inventoryController;

    [Header("Auto-Configuration")]
    [SerializeField]
    [Tooltip("If enabled, will search for the InventoryController in GameSceneManager if it's the local player")]
    private bool useSceneInventoryForLocalPlayer = true;

    [Header("Logging")]
    [SerializeField]
    private Logging.Logger logger;

    private bool isLocalPlayer = false;
    private bool isInitialized = false;

    private void Start() {
        // Initialize in Start instead of Awake to give time for NetworkBehaviour to configure
        // Also try to initialize here as a fallback for single-player
        if (!isInitialized) {
            InitializeInventoryReference();
        }
    }

    /// <summary>
    /// Call this method from NetworkPlayerController.OnNetworkSpawn() to ensure IsOwner is correctly set
    /// </summary>
    public void InitializeForNetworkedPlayer() {
        if (!isInitialized) {
            logger?.Log("[PlayerInventoryReference] InitializeForNetworkedPlayer called from NetworkPlayerController", this);
            InitializeInventoryReference();
        }
    }

    /// <summary>
    /// Initializes the inventory reference. Can be called manually if necessary.
    /// </summary>
    public void InitializeInventoryReference() {
        if (isInitialized) {
            logger?.Log("[PlayerInventoryReference] Already initialized, ignoring duplicate call", this);
            return;
        }

        // Detect if this is the local player
        DetectLocalPlayer();

        // If there's already a valid reference and it's not a prefab, use it
        if (inventoryController != null && inventoryController.gameObject.scene.name != null) {
            logger?.Log($"[PlayerInventoryReference] Reference already configured: {inventoryController.gameObject.name}", this);
            ConfigureInventoryController();
            isInitialized = true;
            return;
        }

        // If it's the local player, search for the inventory in GameSceneManager
        if (isLocalPlayer && useSceneInventoryForLocalPlayer) {
            logger?.Log("[PlayerInventoryReference] Local player detected. Searching for InventoryController in GameSceneManager...", this);

            var gameSceneManager = FindFirstObjectByType<GameSceneManager>();
            if (gameSceneManager != null) {
                // Use reflection to access the private inventoryMenu field
                var field = gameSceneManager.GetType().GetField("inventoryMenu",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (field != null) {
                    inventoryController = field.GetValue(gameSceneManager) as InventoryController;

                    if (inventoryController != null) {
                        logger?.Log($"[PlayerInventoryReference] InventoryController obtained from GameSceneManager: {inventoryController.gameObject.name}", this);
                        ConfigureInventoryController();
                        isInitialized = true;
                        return;
                    }
                }
            }

            logger?.Log("[PlayerInventoryReference] Could not obtain InventoryController from GameSceneManager", this, Logging.LogType.Warning);
        }

        // If it's not the local player or wasn't found in GameSceneManager, instantiate a new one
        if (inventoryPrefab != null) {
            logger?.Log("[PlayerInventoryReference] Instantiating InventoryController from prefab...", this);

            GameObject inventoryGO = Instantiate(inventoryPrefab);
            inventoryGO.name = $"Inventory_{gameObject.name}";
            inventoryController = inventoryGO.GetComponent<InventoryController>();

            if (inventoryController != null) {
                logger?.Log($"[PlayerInventoryReference] InventoryController instantiated: {inventoryGO.name}", this);

                // If it's not the local player, hide the inventory UI and disable the Start() component
                if (!isLocalPlayer) {
                    var uiDocument = inventoryGO.GetComponent<UnityEngine.UIElements.UIDocument>();
                    if (uiDocument != null) {
                        uiDocument.enabled = false;
                        logger?.Log($"[PlayerInventoryReference] Inventory UI disabled for remote player", this);
                    }

                    // Disable the InventoryController for remote players (prevents errors in Start())
                    inventoryController.enabled = false;
                    logger?.Log($"[PlayerInventoryReference] InventoryController disabled for remote player (data storage only)", this);
                }

                ConfigureInventoryController();
                isInitialized = true;
            } else {
                logger?.Log("[PlayerInventoryReference] ERROR: The prefab does not contain an InventoryController!", this, Logging.LogType.Error);
            }
        } else {
            logger?.Log("[PlayerInventoryReference] ERROR: No inventory prefab assigned and could not obtain one from the scene!", this, Logging.LogType.Error);
        }

        isInitialized = true;
    }

    /// <summary>
    /// Detects if this is the local player (the one controlling the current client)
    /// </summary>
    private void DetectLocalPlayer() {
        // Try to detect from Mirror.NetworkIdentity
        var networkIdentity = GetComponent<Mirror.NetworkIdentity>();
        if (networkIdentity != null) {
            isLocalPlayer = networkIdentity.isLocalPlayer;
            logger?.Log($"[PlayerInventoryReference] {(isLocalPlayer ? "LOCAL" : "REMOTE")} player detected (Mirror NetworkIdentity)", this);
            return;
        }

        // Fallback: If there's no NetworkIdentity, assume it's local (single-player mode)
        isLocalPlayer = true;
        logger?.Log($"[PlayerInventoryReference] NetworkIdentity not found. Assuming LOCAL player (single-player)", this);
    }

    /// <summary>
    /// Configures the InventoryController with the player's PlayerInputReader
    /// </summary>
    private void ConfigureInventoryController() {
        if (inventoryController == null) return;

        // PlayerInputReader is a ScriptableObject, not a component
        // We need to find it in PlayerController or NetworkPlayerController
        PlayerInputReader playerInputReader = null;

        // Try to get from PlayerController (single-player)
        var playerController = GetComponent<PlayerController>();
        if (playerController != null) {
            playerInputReader = playerController.inputReader;
        }

        // If not found, try from NetworkPlayerController (multiplayer)
        if (playerInputReader == null) {
            var networkPlayerController = GetComponent<NetworkPlayerController>();
            if (networkPlayerController != null) {
                // NetworkPlayerController has a private serialized field, use reflection
                var field = networkPlayerController.GetType().GetField("playerInputReader",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (field != null) {
                    playerInputReader = field.GetValue(networkPlayerController) as PlayerInputReader;
                }
            }
        }

        if (playerInputReader != null) {
            // Use reflection to assign the playerInputReader to the InventoryController
            var field = inventoryController.GetType().GetField("playerInputReader",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (field != null) {
                field.SetValue(inventoryController, playerInputReader);
                logger?.Log($"[PlayerInventoryReference] Player '{gameObject.name}' PlayerInputReader assigned to InventoryController", this);
            }
        } else {
            logger?.Log($"[PlayerInventoryReference] WARNING: PlayerInputReader not found in {gameObject.name}. Make sure PlayerController or NetworkPlayerController is present.", this, Logging.LogType.Warning);
        }
    }

    /// <summary>
    /// Gets the InventoryController associated with this player
    /// </summary>
    /// <returns>The player's InventoryController, or null if not assigned</returns>
    public InventoryController GetInventory() {
        if (inventoryController == null) {
            logger?.Log("[PlayerInventoryReference] Error: Trying to access a null inventory!", this, Logging.LogType.Error);
        }
        return inventoryController;
    }

    /// <summary>
    /// Sets the reference to the InventoryController (useful for dynamic configuration)
    /// </summary>
    /// <param name="inventory">The InventoryController to assign</param>
    public void SetInventory(InventoryController inventory) {
        inventoryController = inventory;
        logger?.Log($"[PlayerInventoryReference] Inventory dynamically assigned for {gameObject.name}", this);
    }

    /// <summary>
    /// Checks if there's a valid inventory assigned
    /// </summary>
    /// <returns>True if there's an inventory assigned, false if not</returns>
    public bool HasInventory() {
        return inventoryController != null;
    }
}
