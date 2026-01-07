using System.Collections.Generic;
using Mirror;
using Models;
using UnityEngine;

/// <summary>
/// Component added to enemies to drop loot upon death.
/// Subscribes to the OnDeath event of the HealthComponent.
/// Compatible with single-player and multiplayer (Mirror).
///
/// Mirror implementation:
/// - Server spawns loot using NetworkServer.Spawn()
/// - Works with LootItem's SyncList to synchronize items
/// - Only server should execute loot spawning logic
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class LootDropper : MonoBehaviour
{
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

    private void Awake()
    {
        healthComponent = GetComponent<HealthComponent>();

        if (healthComponent == null)
        {
            logger?.Log(
                "[LootDropper] Error: HealthComponent not found on the enemy!",
                this,
                Logging.LogType.Error
            );
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDeath -= HandleDeath;
        }
    }

    /// <summary>
    /// Executes when the enemy dies
    /// </summary>
    private void HandleDeath()
    {
        // In multiplayer, only the server should spawn loot
        if (NetworkServer.active || NetworkClient.active)
        {
            if (!NetworkServer.active)
            {
                logger?.Log(
                    "[LootDropper] Client ignoring HandleDeath - only the server spawns loot",
                    this
                );
                return;
            }
        }

        if (alwaysDrop && lootPrefab != null)
        {
            DropLoot();
        }
        else if (lootPrefab == null)
        {
            logger?.Log(
                "[LootDropper] Warning: No loot prefab assigned!",
                this,
                Logging.LogType.Warning
            );
        }
    }

    /// <summary>
    /// Instantiates the loot at the enemy's position
    /// </summary>
    private void DropLoot()
    {
        // Calculate spawn position with random offset
        Vector3 spawnPosition = transform.position + spawnOffset;

        if (randomOffset > 0)
        {
            Vector2 randomCircle = Random.insideUnitCircle * randomOffset;
            spawnPosition += new Vector3(randomCircle.x, 0, randomCircle.y);
        }

        // Instantiate the loot
        GameObject lootInstance = Instantiate(lootPrefab, spawnPosition, Quaternion.identity);

        // Check components
        LootItem lootItem = lootInstance.GetComponent<LootItem>();
        if (lootItem == null)
        {
            logger?.Log(
                $"[LootDropper] ERROR: Loot prefab does not have LootItem component!",
                this,
                Logging.LogType.Error
            );
            Destroy(lootInstance);
            return;
        }

        // Initialize position (does not touch SyncList)
        lootItem.Initialize(spawnPosition);

        // Determine item IDs and gold amount for this loot bag
        List<string> lootItemIds = GetRandomLootItems();
        int goldAmount = GetRandomGoldAmount();
        bool isMultiplayer = NetworkServer.active || NetworkClient.active;

        if (isMultiplayer)
        {
            NetworkIdentity networkIdentity = lootInstance.GetComponent<NetworkIdentity>();
            if (networkIdentity == null)
            {
                logger?.Log(
                    $"[LootDropper] ERROR: Loot prefab does not have NetworkIdentity! The loot will not be visible in multiplayer.",
                    this,
                    Logging.LogType.Error
                );
                Destroy(lootInstance);
                return;
            }

            // First spawn on the network, then configure SyncList on server
            NetworkServer.Spawn(lootInstance);
            logger?.Log($"[LootDropper] Loot spawned as NetworkIdentity at {spawnPosition}", this);
        }

        // Configure items and gold AFTER the NetworkIdentity has been spawned
        if (lootItemIds != null && lootItemIds.Count > 0)
        {
            lootItem.SetItemIds(lootItemIds);
            logger?.Log(
                $"[LootDropper] Configured loot with {lootItemIds.Count} item IDs: {string.Join(", ", lootItemIds)}",
                this
            );
        }
        else
        {
            logger?.Log(
                "[LootDropper] WARNING: No items obtained for loot bag - loot will contain only gold (if any).",
                this,
                Logging.LogType.Warning
            );
        }

        if (goldAmount > 0)
        {
            lootItem.SetGoldAmount(goldAmount);
            logger?.Log($"[LootDropper] Configured loot with {goldAmount} gold.", this);
        }
    }

    /// <summary>
    /// Get loot item IDs based on current world's enemy loot configuration.
    /// Uses EnemyData.lootItems and returns spriteId strings as item IDs.
    /// </summary>
    private List<string> GetRandomLootItems()
    {
        var result = new List<string>();
        var worldData = Worlds.WorldItemsRegistry.CurrentWorldData;

        if (worldData == null)
        {
            logger?.Log(
                "[LootDropper] No world data registered in WorldItemsRegistry. Loot will not drop.",
                this,
                Logging.LogType.Warning
            );
            return result;
        }

        if (worldData.enemies == null || worldData.enemies.Count == 0)
        {
            logger?.Log(
                "[LootDropper] World has no enemies configured. No loot will drop.",
                this,
                Logging.LogType.Warning
            );
            return result;
        }

        // For now, if spawn areas don't specify an enemy, use the first one.
        EnemyData enemyData = worldData.enemies[0];

        if (
            enemyData.lootTable == null
            || enemyData.lootTable.lootItems == null
            || enemyData.lootTable.lootItems.Count == 0
        )
        {
            logger?.Log(
                $"[LootDropper] Enemy '{enemyData.name}' has no lootTable or lootItems configured.",
                this,
                Logging.LogType.Warning
            );
            return result;
        }

        foreach (LootEntryData loot in enemyData.lootTable.lootItems)
        {
            if (loot == null)
            {
                continue;
            }

            if (loot.dropProbability <= 0 || string.IsNullOrEmpty(loot.id))
            {
                continue;
            }

            int roll = UnityEngine.Random.Range(0, 100);
            if (roll >= loot.dropProbability)
            {
                logger?.Log(
                    $"[LootDropper] Loot '{loot.name}' (id={loot.id}) did not drop. Roll={roll}, chance={loot.dropProbability}",
                    this
                );
                continue;
            }

            // For compatibility, drop at least one, but you can add a maxAmount field to LootEntryData if needed
            int count = 1;

            for (int i = 0; i < count; i++)
            {
                result.Add(loot.id);
            }

            logger?.Log($"[LootDropper] Loot '{loot.name}' (id={loot.id}) dropped x{count}.", this);
        }

        if (result.Count == 0)
        {
            logger?.Log(
                "[LootDropper] Loot table evaluated but no items were selected to drop.",
                this,
                Logging.LogType.Warning
            );
        }

        return result;
    }

    /// <summary>
    /// Get a random gold amount for this loot drop based on the enemy's loot table configuration.
    /// Returns an integer between minGoldDropAmount and maxGoldDropAmount (inclusive).
    /// If loot table is not configured or max <= 0, returns 0.
    /// </summary>
    private int GetRandomGoldAmount()
    {
        var worldData = Worlds.WorldItemsRegistry.CurrentWorldData;

        if (worldData == null || worldData.enemies == null || worldData.enemies.Count == 0)
        {
            return 0;
        }

        // For now, mirror the same enemy selection logic as GetRandomLootItems (first enemy)
        EnemyData enemyData = worldData.enemies[0];

        if (enemyData == null || enemyData.lootTable == null)
        {
            return 0;
        }

        int minGold = Mathf.Max(0, enemyData.lootTable.minGoldDropAmount);
        int maxGold = Mathf.Max(0, enemyData.lootTable.maxGoldDropAmount);

        if (maxGold <= 0)
        {
            return 0;
        }

        if (minGold > maxGold)
        {
            // Swap if misconfigured
            int temp = minGold;
            minGold = maxGold;
            maxGold = temp;
        }

        int gold = UnityEngine.Random.Range(minGold, maxGold + 1);

        logger?.Log($"[LootDropper] Gold roll between {minGold} and {maxGold}: {gold}", this);

        return gold;
    }

#if UNITY_EDITOR
    // Visualization in the editor to see where the loot will drop
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 dropPosition = transform.position + spawnOffset;
        Gizmos.DrawWireSphere(dropPosition, 0.2f);

        // Show the random spawn area
        if (randomOffset > 0)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(dropPosition, randomOffset);
        }
    }
#endif
}
