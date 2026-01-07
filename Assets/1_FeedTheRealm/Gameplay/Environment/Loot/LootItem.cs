using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Represents a loot item (bag) that appears in the world when an enemy dies.
/// When approaching, it automatically transfers the items to the player's inventory.
/// Server-authoritative implementation to prevent cheating and duplication bugs.
///
/// Mirror implementation:
/// - Server has full authority over item transfer and validation
/// - Client requests pickup via [Command]
/// - Server validates, transfers items, and notifies client via [TargetRpc]
/// - SyncList tracks available items
/// </summary>
public class LootItem : NetworkBehaviour
{
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

    [Header("Pickup Settings")]
    [SerializeField]
    [Tooltip("Pickup radius (trigger zone size)")]
    private float pickupRadius = 1.5f;

    [SerializeField]
    [Tooltip("Layer mask for detecting the player")]
    private LayerMask playerLayer;

    [SerializeField]
    [Tooltip("Delay in seconds before loot becomes collectible")]
    private float lootableDelay = 1.0f;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    // SyncList to synchronize item IDs between server and clients
    private readonly SyncList<string> _itemIds = new SyncList<string>();

    // Gold amount contained in this loot bag (server-authoritative)
    [SyncVar]
    private int _goldAmount;

    private SphereCollider _triggerCollider;

    // Prevent multiple simultaneous pickups
    private bool _isBeingPickedUp = false;
    private HashSet<uint> _playersWhoTriedPickup = new HashSet<uint>();

    // Delay to avoid immediate loot after spawn
    private float _spawnTime;
    private bool _isLootable = false;

    /// <summary>
    /// Sets the gold amount contained in this loot bag. Only the server should call this.
    /// </summary>
    [Server]
    public void SetGoldAmount(int amount)
    {
        if (!isServer)
        {
            return;
        }

        _goldAmount = Mathf.Max(0, amount);

        logger?.Log($"[LootItem] Gold amount set to {_goldAmount}", this);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        logger?.Log($"[LootItem] OnStartServer - ItemCount: {_itemIds.Count}", this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        logger?.Log(
            $"[LootItem] OnStartClient - isServer: {isServer}, ItemCount: {_itemIds.Count}, Items: {string.Join(", ", _itemIds)}",
            this
        );

        if (!isServer)
        {
            StartCoroutine(WaitForItemsManagerAndUpdateVisuals());
        }
    }

    public override void OnStopClient()
    {
        if (_itemIds != null)
        {
            _itemIds.Callback -= OnItemListChanged;
        }
    }

    private void OnItemListChanged(
        SyncList<string>.Operation op,
        int index,
        string oldItem,
        string newItem
    )
    {
        if (isClient && !isServer)
        {
            UpdateVisualsFromManager();
        }
    }

    private System.Collections.IEnumerator WaitForItemsManagerAndUpdateVisuals()
    {
        // Subscribe to changes
        _itemIds.Callback += OnItemListChanged;

        // Update visuals
        UpdateVisualsFromManager();
        yield break;
    }

    private void UpdateVisualsFromManager()
    {
        logger?.Log($"[LootItem] Updating visuals for {_itemIds.Count} items", this);

        // Now items are identified by their unique item id (not spriteId)
        foreach (var itemId in _itemIds)
        {
            var consumable = Worlds.WorldItemsRegistry.GetConsumableById(itemId);
            if (consumable != null)
            {
                logger?.Log(
                    $"[LootItem] Contains world item: {consumable.name} (id={itemId})",
                    this
                );
            }
            else
            {
                logger?.Log(
                    $"[LootItem] Item id in bag but no consumable found: {itemId}",
                    this,
                    Logging.LogType.Warning
                );
            }
        }
    }

    private void Start()
    {
        if (heightOffset != 0)
        {
            transform.position += Vector3.up * heightOffset;
        }

        if (itemVisual == null)
        {
            logger?.Log(
                $"[LootItem] Warning: No visual assigned for loot '{itemName}'",
                this,
                Logging.LogType.Warning
            );
        }
        else
        {
            logger?.Log(
                $"[LootItem] Loot '{itemName}' spawned at {transform.position} with {_itemIds.Count} item IDs",
                this
            );
        }

        SetupTriggerCollider();
    }

    /// <summary>
    /// Sets up the trigger collider as a child to detect players
    /// </summary>
    private void SetupTriggerCollider()
    {
        GameObject triggerObj = new GameObject("PickupTrigger");
        triggerObj.transform.SetParent(transform);
        triggerObj.transform.localPosition = Vector3.zero;
        triggerObj.layer = gameObject.layer;

        _triggerCollider = triggerObj.AddComponent<SphereCollider>();
        _triggerCollider.isTrigger = true;
        _triggerCollider.radius = pickupRadius;

        logger?.Log($"[LootItem] Trigger collider configured with radius {pickupRadius}", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        bool isNetworked = NetworkClient.active || NetworkServer.active;

        if (isNetworked && !isServer)
        {
            return;
        }

        if (((1 << other.gameObject.layer) & playerLayer) == 0)
            return;

        if (!_isLootable)
            return;

        if (_isBeingPickedUp)
            return;

        PlayerInventoryReference inventoryRef = other.GetComponent<PlayerInventoryReference>();
        if (inventoryRef == null)
            return;

        if (isNetworked)
        {
            NetworkIdentity playerIdentity = other.GetComponent<NetworkIdentity>();
            if (playerIdentity == null)
                return;

            if (_playersWhoTriedPickup.Contains(playerIdentity.netId))
                return;

            _playersWhoTriedPickup.Add(playerIdentity.netId);
            logger?.Log(
                $"[LootItem] SERVER - Player {playerIdentity.netId} triggered pickup",
                this
            );
        }
        else
        {
            logger?.Log($"[LootItem] LOCAL - Player triggered pickup", this);
        }

        _isBeingPickedUp = true;

        ProcessPickup(inventoryRef);
    }

    /// <summary>
    /// Processes the pickup, validates, transfers items (works for both networked and local)
    /// </summary>
    private void ProcessPickup(PlayerInventoryReference inventoryRef)
    {
        bool isNetworked = NetworkClient.active || NetworkServer.active;

        if (_itemIds.Count == 0 && _goldAmount <= 0)
        {
            logger?.Log(
                $"[LootItem] Bag is empty (no items or gold), {(isNetworked ? "despawning" : "destroying")}",
                this
            );
            if (isNetworked)
                NetworkServer.Destroy(gameObject);
            else
                Destroy(gameObject);
            return;
        }

        // Get all items from the bag
        List<string> itemsToTransfer = new List<string>(_itemIds);

        int goldToTransfer = _goldAmount;

        logger?.Log(
            $"[LootItem] Transferring {itemsToTransfer.Count} items and {goldToTransfer} gold to player",
            this
        );

        // Clear the bag contents on server/local
        _itemIds.Clear();
        _goldAmount = 0;

        if (isNetworked)
        {
            // Networked: Get identity and use TargetRpc for items; gold is handled server-side
            NetworkIdentity playerIdentity = inventoryRef.GetComponent<NetworkIdentity>();
            if (playerIdentity != null)
            {
                // Add gold to the player's gold component on the server
                PlayerGold playerGold = inventoryRef.GetComponent<PlayerGold>();
                if (playerGold != null)
                {
                    playerGold.AddGold(goldToTransfer);
                    logger?.Log(
                        $"[LootItem] SERVER - Added {goldToTransfer} gold to player {playerIdentity.netId}",
                        this
                    );
                }
                else if (goldToTransfer > 0)
                {
                    logger?.Log(
                        "[LootItem] SERVER - PlayerGold component not found on player while transferring gold.",
                        this,
                        Logging.LogType.Warning
                    );
                }

                TargetReceiveLoot(playerIdentity.connectionToClient, itemsToTransfer);
            }
            NetworkServer.Destroy(gameObject);
        }
        else
        {
            // Local (non-networked): Add items directly and apply gold locally
            InventoryController inventory = inventoryRef.GetInventory();
            if (inventory == null)
            {
                logger?.Log(
                    "[LootItem] LOCAL - ERROR: InventoryController not found",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            int itemsAdded = 0;
            foreach (string itemId in itemsToTransfer)
            {
                if (inventory.IsInventoryFull())
                {
                    logger?.Log(
                        $"[LootItem] LOCAL - Inventory full! Added {itemsAdded}/{itemsToTransfer.Count} items",
                        this,
                        Logging.LogType.Warning
                    );
                    break;
                }

                inventory.AddItemById(itemId);
                itemsAdded++;
                logger?.Log($"[LootItem] LOCAL - Added item to inventory: {itemId}", this);
            }

            logger?.Log(
                $"[LootItem] LOCAL - Successfully added {itemsAdded} items to inventory",
                this
            );

            // Local gold handling: if there is a PlayerGold component, update it as well
            PlayerGold localPlayerGold = inventoryRef.GetComponent<PlayerGold>();
            if (localPlayerGold != null)
            {
                localPlayerGold.AddGold(goldToTransfer);
                logger?.Log($"[LootItem] LOCAL - Added {goldToTransfer} gold to player.", this);
            }

            // Destroy the loot bag
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// TARGET RPC: Tells a specific client to add items to their inventory
    /// This ensures only the player who picked up gets the items
    /// </summary>
    [TargetRpc]
    private void TargetReceiveLoot(NetworkConnectionToClient conn, List<string> receivedItems)
    {
        logger?.Log($"[LootItem] CLIENT - Received {receivedItems.Count} items from server", this);

        // Find the local player's inventory using NetworkClient.localPlayer
        NetworkIdentity localPlayer = NetworkClient.localPlayer;
        if (localPlayer == null)
        {
            logger?.Log(
                "[LootItem] CLIENT - ERROR: No local player identity",
                this,
                Logging.LogType.Error
            );
            return;
        }

        PlayerInventoryReference inventoryRef =
            localPlayer.GetComponent<PlayerInventoryReference>();
        if (inventoryRef == null)
        {
            logger?.Log(
                "[LootItem] CLIENT - ERROR: PlayerInventoryReference not found",
                this,
                Logging.LogType.Error
            );
            return;
        }

        InventoryController inventory = inventoryRef.GetInventory();
        if (inventory == null)
        {
            logger?.Log(
                "[LootItem] CLIENT - ERROR: InventoryController not found",
                this,
                Logging.LogType.Error
            );
            return;
        }

        // Add items to inventory
        int itemsAdded = 0;
        foreach (string itemId in receivedItems)
        {
            if (inventory.IsInventoryFull())
            {
                logger?.Log(
                    $"[LootItem] CLIENT - Inventory full! Added {itemsAdded}/{receivedItems.Count} items",
                    this,
                    Logging.LogType.Warning
                );
                break;
            }

            inventory.AddItemById(itemId);
            itemsAdded++;
            logger?.Log($"[LootItem] CLIENT - Added item to inventory: {itemId}", this);
        }

        logger?.Log(
            $"[LootItem] CLIENT - Successfully added {itemsAdded} items to inventory",
            this
        );
    }

    /// <summary>
    /// Initializes the loot item at a specific position
    /// </summary>
    /// <param name="spawnPosition">Position where the loot will appear</param>
    public void Initialize(Vector3 spawnPosition)
    {
        transform.position = spawnPosition + Vector3.up * heightOffset;

        // Record spawn time and start delay
        _spawnTime = Time.time;
        _isLootable = false;

        // Start coroutine to enable loot after delay
        StartCoroutine(EnableLootAfterDelay());
    }

    /// <summary>
    /// Coroutine that waits for the delay before allowing the loot to be collected
    /// </summary>
    private System.Collections.IEnumerator EnableLootAfterDelay()
    {
        yield return new WaitForSeconds(lootableDelay);

        _isLootable = true;
        logger?.Log($"[LootItem] Loot is now lootable", this);

        bool isNetworked = NetworkClient.active || NetworkServer.active;
        if (isServer || !isNetworked)
        {
            CheckForPlayersInRange();
        }
    }

    /// <summary>
    /// Manually checks for players in range using Physics.OverlapSphere
    /// This handles the case where loot spawns on top of a player (OnTriggerEnter won't fire)
    /// </summary>
    private void CheckForPlayersInRange()
    {
        bool isNetworked = NetworkClient.active || NetworkServer.active;
        if (isNetworked && !isServer)
            return;
        if (_isBeingPickedUp)
            return;

        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRadius, playerLayer);

        if (colliders.Length > 0)
        {
            logger?.Log(
                $"[LootItem] Found {colliders.Length} player(s) already in range after becoming lootable",
                this
            );

            // Process pickup for the first valid player found
            foreach (Collider col in colliders)
            {
                PlayerInventoryReference inventoryRef =
                    col.GetComponent<PlayerInventoryReference>();
                if (inventoryRef == null)
                    continue;

                if (isNetworked)
                {
                    NetworkIdentity playerIdentity = col.GetComponent<NetworkIdentity>();
                    if (playerIdentity == null)
                        continue;

                    if (_playersWhoTriedPickup.Contains(playerIdentity.netId))
                        continue;

                    _playersWhoTriedPickup.Add(playerIdentity.netId);
                    logger?.Log(
                        $"[LootItem] SERVER - Player {playerIdentity.netId} was already in range, processing pickup",
                        this
                    );
                }
                else
                {
                    logger?.Log(
                        $"[LootItem] LOCAL - Player was already in range, processing pickup",
                        this
                    );
                }

                _isBeingPickedUp = true;

                ProcessPickup(inventoryRef);
                break;
            }
        }
    }

    /// <summary>
    /// Sets up the item IDs for this loot bag.
    /// Call AFTER NetworkServer.Spawn() in multiplayer so SyncList is initialized.
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

        if (ids == null || ids.Count == 0)
        {
            logger?.Log(
                "[LootItem] SetItemIds called with empty or null list",
                this,
                Logging.LogType.Warning
            );
            return;
        }

        foreach (var id in ids)
        {
            if (!string.IsNullOrEmpty(id))
            {
                _itemIds.Add(id);
            }
        }

        logger?.Log($"[LootItem] Configured {_itemIds.Count} item IDs", this);
    }

    /// <summary>
    /// Changes the loot visual at runtime (useful for different types of loot)
    /// </summary>
    /// <param name="newVisual">The new visual GameObject</param>
    public void SetVisual(GameObject newVisual)
    {
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
