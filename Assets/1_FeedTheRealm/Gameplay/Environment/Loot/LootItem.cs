using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a loot item (bag) that appears in the world when an enemy dies.
/// When approaching, it automatically transfers the items to the player's inventory.
/// Compatible with single-player and multiplayer (Mirror).
///
/// Mirror implementation:
/// - Uses SyncList<string> to synchronize item IDs between server and clients
/// - Server has authority over loot spawning and despawning
/// - Clients handle inventory UI updates and visual feedback
/// - [Command] for client requests to server
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
    // SyncList to synchronize item IDs between server and clients
    private readonly SyncList<string> itemIds = new SyncList<string>();

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

    // Delay to avoid immediate loot after spawn
    private float spawnTime;
    private bool isLootable = false;
    [SerializeField]
    [Tooltip("Delay in seconds before loot becomes collectible")]
    private float lootableDelay = 1.0f;

    public override void OnStartServer()
    {
        base.OnStartServer();
        logger?.Log($"[LootItem] OnStartServer - ItemCount: {itemIds.Count}", this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        logger?.Log($"[LootItem] OnStartClient - isServer: {isServer}, ItemCount: {itemIds.Count}, Items: {string.Join(", ", itemIds)}", this);

        // Clients: Wait and update visuals
        if (!isServer)
        {
            StartCoroutine(WaitForItemsManagerAndUpdateVisuals());
        }
    }

    public override void OnStopClient()
    {
        if (itemIds != null)
        {
            itemIds.Callback -= OnItemListChanged;
        }
    }

    private void OnItemListChanged(SyncList<string>.Operation op, int index, string oldItem, string newItem)
    {
        // Re-synchronize visuals when the list changes
        if (isClient && !isServer)
        {
            UpdateVisualsFromManager();
        }
    }

    private System.Collections.IEnumerator WaitForItemsManagerAndUpdateVisuals()
    {
        // Wait for ItemsManager to be initialized
        while (Items.ItemsManager.Instance == null || !Items.ItemsManager.Instance.IsInitialized)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Subscribe to changes
        itemIds.Callback += OnItemListChanged;

        // Update visuals
        UpdateVisualsFromManager();
    }

    private string GetFirstItemId()
    {
        if (itemIds != null && itemIds.Count > 0)
        {
            return itemIds[0];
        }
        return "";
    }

    private void UpdateVisualsFromManager()
    {
        if (Items.ItemsManager.Instance == null || !Items.ItemsManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[LootItem] ItemsManager not available or not initialized");
            return;
        }

        logger?.Log($"[LootItem] Updating visuals for {itemIds.Count} items", this);

        // Here you could update the loot bag visual based on the items
        // For now we just log
        foreach (var itemId in itemIds)
        {
            var metadata = Items.ItemsManager.Instance.GetItemById(itemId);
            if (metadata != null)
            {
                logger?.Log($"[LootItem] Contains: {metadata.name}", this);
            }
        }
    }

    private void Start()
    {
        // Adjust vertical position if necessary
        if (heightOffset != 0)
        {
            transform.position += Vector3.up * heightOffset;
        }

        // Verify that a visual is assigned
        if (itemVisual == null)
        {
            logger?.Log($"[LootItem] Warning: No visual assigned for loot '{itemName}'", this, Logging.LogType.Warning);
        }
        else
        {
            logger?.Log($"[LootItem] Loot '{itemName}' spawned at {transform.position} with {itemIds.Count} item IDs", this);
        }

        // Create the trigger collider for player detection
        SetupTriggerCollider();
    }

    /// <summary>
    /// Sets up the trigger collider as a child to detect players
    /// </summary>
    private void SetupTriggerCollider()
    {
        // Create a child GameObject for the trigger
        GameObject triggerObj = new GameObject("PickupTrigger");
        triggerObj.transform.SetParent(transform);
        triggerObj.transform.localPosition = Vector3.zero;
        triggerObj.layer = gameObject.layer;

        // Add and configure the SphereCollider as a trigger
        triggerCollider = triggerObj.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = pickupRadius;

        logger?.Log($"[LootItem] Trigger collider configured with radius {pickupRadius}", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if it's the player
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            //logger?.Log($"[LootItem] Player detected: {other.name}", this);

            playersInRange.Add(other);

            // Only try to pick up if the loot is already lootable
            if (isLootable)
            {
                TryPickupItems(other.gameObject);
            }
            else
            {
                float timeSinceSpawn = Time.time - spawnTime;
                //logger?.Log($"[LootItem] Loot not lootable yet. Time elapsed: {timeSinceSpawn:F2}s of {lootableDelay}s", this);
            }
        }
    }

    private void Update()
    {
        // If there are players in range but the loot is not lootable yet, check periodically
        if (!isLootable && playersInRange.Count > 0)
        {
            if (Time.time - spawnTime >= lootableDelay)
            {
                isLootable = true;
                //logger?.Log($"[LootItem] Loot became lootable while there were players in range", this);

                // Try to pick up for all players in range
                foreach (var playerCollider in playersInRange)
                {
                    if (playerCollider != null && playerCollider.gameObject != null)
                    {
                        TryPickupItems(playerCollider.gameObject);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Attempts to transfer the items to the player's inventory
    /// </summary>
    private void TryPickupItems(GameObject player)
    {
        //logger?.Log($"[LootItem] TryPickupItems called - IsLootable: {isLootable}, ItemCount: {itemIds.Count}, TimeSinceSpawn: {Time.time - spawnTime:F2}s", this);

        // Avoid multiple processing if already attempted and failed
        if (hasAttemptedPickup)
        {
            //logger?.Log("[LootItem] TryPickupItems - Already attempted pickup, skipping", this);
            return;
        }

        // SERVER/CLIENT SEPARATION: Different responsibilities
        bool isMultiplayer = NetworkServer.active || NetworkClient.active;
        NetworkIdentity playerNetworkIdentity = player.GetComponent<NetworkIdentity>();

        if (isMultiplayer && isServer)
        {
            // ============================================
            // SERVER: Handles SyncList and despawn
            // ============================================
            Debug.Log($"[LootItem] SERVER - Processing pickup. Items in bag: {itemIds.Count}");

            // Check if there are items to transfer
            if (itemIds.Count == 0)
            {
                Debug.Log($"[LootItem] SERVER - Bag empty, despawning");
                NetworkServer.Destroy(gameObject);
                return;
            }

            // Remove all items from SyncList (server has authority)
            int itemsRemoved = itemIds.Count;
            itemIds.Clear();

            Debug.Log($"[LootItem] SERVER - Removed {itemsRemoved} items from SyncList. Despawning...");

            // Despawn the network object
            NetworkServer.Destroy(gameObject);

        }
        else
        {
            // ============================================
            // CLIENT: Handles inventory UI and feedback
            // ============================================

            // Only process if we are the owner of this player (our local client)
            if (isMultiplayer && playerNetworkIdentity != null && !playerNetworkIdentity.isLocalPlayer)
            {
                return;
            }

            Debug.Log($"[LootItem] CLIENT - Processing pickup for local player {player.name}");

            // Find the PlayerInventoryReference on the player
            PlayerInventoryReference inventoryRef = player.GetComponent<PlayerInventoryReference>();

            if (inventoryRef == null)
            {
                logger?.Log($"[LootItem] PlayerInventoryReference not found on {player.name}", this, Logging.LogType.Warning);
                hasAttemptedPickup = true;
                return;
            }

            // Get the InventoryController from the reference
            InventoryController inventory = inventoryRef.GetInventory();

            if (inventory == null)
            {
                logger?.Log($"[LootItem] PlayerInventoryReference has no InventoryController assigned on {player.name}", this, Logging.LogType.Warning);
                hasAttemptedPickup = true;
                return;
            }

            // Check if there are items to transfer
            if (itemIds.Count == 0)
            {
                logger?.Log($"[LootItem] CLIENT - Bag empty (synchronized from server)", this);
                hasAttemptedPickup = true;
                return;
            }

            logger?.Log($"[LootItem] CLIENT - Bag has {itemIds.Count} items, proceeding with transfer to UI", this);

            // Count how many empty slots there are
            int emptySlots = inventory.GetEmptySlotCount();
            logger?.Log($"[LootItem] CLIENT - Empty slots: {emptySlots}, Inventory full: {inventory.IsInventoryFull()}", this);

            // If no space, mark as attempted and do nothing
            if (emptySlots == 0)
            {
                logger?.Log($"[LootItem] CLIENT - Inventory full! Cannot pick up loot.", this, Logging.LogType.Warning);
                hasAttemptedPickup = true;
                return;
            }

            // Transfer items one by one until filling the inventory or emptying the bag
            int itemsTransferred = 0;

            // Convert SyncList to temporary list to iterate
            List<string> itemIdsList = new List<string>(itemIds);

            foreach (string itemId in itemIdsList)
            {
                if (inventory.IsInventoryFull())
                {
                    logger?.Log($"[LootItem] CLIENT - Inventory full. Items transferred: {itemsTransferred}/{itemIds.Count}", this);
                    hasAttemptedPickup = true;
                    break;
                }

                // Transfer item by ID to inventory UI
                // InventoryController will handle sprite loading from ItemsManager
                inventory.AddItemById(itemId);
                itemsTransferred++;

                logger?.Log($"[LootItem] CLIENT - Item transferred to UI: {itemId}", this);
            }

            logger?.Log($"[LootItem] CLIENT - Transfer completed: {itemsTransferred} items to inventory", this);

            // Play visual/sound feedback
            if (itemsTransferred > 0)
            {
                PlayPickupFeedback();
            }

            // If the inventory filled before emptying the bag, mark as attempted
            if (itemsTransferred < itemIdsList.Count)
            {
                logger?.Log($"[LootItem] CLIENT - Inventory full, not all items could be transferred", this);
                hasAttemptedPickup = true;
            }
        }
    }

    /// <summary>
    /// Plays sound effects and visuals when picking up
    /// </summary>
    private void PlayPickupFeedback()
    {
        // Play sound if assigned
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            logger?.Log($"[LootItem] Playing pickup sound", this);
        }

        // Instantiate VFX if assigned
        if (pickupVFX != null)
        {
            Instantiate(pickupVFX, transform.position, Quaternion.identity);
            logger?.Log($"[LootItem] Spawning pickup VFX", this);
        }
    }

    /// <summary>
    /// Initializes the loot item at a specific position
    /// </summary>
    /// <param name="spawnPosition">Position where the loot will appear</param>
    public void Initialize(Vector3 spawnPosition)
    {
        transform.position = spawnPosition + Vector3.up * heightOffset;

        // Record spawn time and start delay
        spawnTime = Time.time;
        isLootable = false;

        // Start coroutine to enable loot after delay
        StartCoroutine(EnableLootAfterDelay());

        //logger?.Log($"[LootItem] Initialized at {spawnPosition}. Loot will be lootable in {lootableDelay}s", this);
    }

    /// <summary>
    /// Coroutine that waits for the delay before allowing the loot to be collected
    /// </summary>
    private System.Collections.IEnumerator EnableLootAfterDelay()
    {
        yield return new WaitForSeconds(lootableDelay);

        isLootable = true;
        //logger?.Log($"[LootItem] Loot is now lootable after {lootableDelay}s", this);
    }

    /// <summary>
    /// Sets up the item IDs for this loot bag.
    /// IMPORTANT: Call AFTER NetworkServer.Spawn() in multiplayer so SyncList is initialized.
    /// Only the server should call this method.
    /// </summary>
    /// <param name="ids">List of item IDs to set up</param>
    public void SetItemIds(List<string> ids)
    {
        if (!isServer)
        {
            Debug.LogWarning("[LootItem] SetItemIds should only be called by server!");
            return;
        }

        //logger?.Log($"[LootItem] SetItemIds called with {ids?.Count ?? 0} items: {string.Join(", ", ids ?? new List<string>())}", this);

        if (ids == null || ids.Count == 0)
        {
            logger?.Log("[LootItem] SetItemIds called with empty or null list", this, Logging.LogType.Warning);
            return;
        }

        foreach (var id in ids)
        {
            if (!string.IsNullOrEmpty(id))
            {
                itemIds.Add(id);
            }
        }

        logger?.Log($"[LootItem] Configured {itemIds.Count} item IDs", this);
    }

    /// <summary>
    /// Changes the loot visual at runtime (useful for different types of loot)
    /// </summary>
    /// <param name="newVisual">The new visual GameObject</param>
    public void SetVisual(GameObject newVisual)
    {
        // Destroy the previous visual if it exists
        if (itemVisual != null)
        {
            Destroy(itemVisual);
        }

        itemVisual = Instantiate(newVisual, transform);
        itemVisual.transform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// Sets the item name (useful for debug and future features)
    /// </summary>
    /// <param name="name">Item name</param>
    public void SetItemName(string name)
    {
        itemName = name;
        gameObject.name = $"Loot_{name}";
    }

#if UNITY_EDITOR
    // Visualization in the editor to facilitate debugging
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // Draw the pickup radius
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
#endif
}
