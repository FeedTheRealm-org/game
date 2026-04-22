using System.Collections.Generic;
using Assets.HeroEditor4D.InventorySystem.Scripts.Enums;
using FTR.Core.Server.Enums;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Server.Registry
{
    /// <summary>
    /// Static registry that exposes the current world's items (consumables, weapons, etc.),
    /// enemies, and loot tables to server-side gameplay systems (combat, dropping loot).
    /// </summary>
    public static class ServerItemsRegistry
    {
        public static CreatablesData CurrentData { get; private set; }

        private static readonly Dictionary<string, ItemData> itemsById =
            new Dictionary<string, ItemData>();
        private static readonly Dictionary<string, EnemyData> enemiesById =
            new Dictionary<string, EnemyData>();
        private static readonly Dictionary<string, LootTableData> lootTablesById =
            new Dictionary<string, LootTableData>();
        private static readonly HashSet<string> worldItemIds = new HashSet<string>();

        public static void RegisterWorldData(CreatablesData data)
        {
            CurrentData = data;

            itemsById.Clear();
            enemiesById.Clear();
            lootTablesById.Clear();
            worldItemIds.Clear();

            if (data == null)
            {
                Debug.LogWarning("[ServerItemsRegistry] RegisterWorldData called with null data");
                return;
            }

            int registeredItems = 0;

            if (data.consumableItems != null)
            {
                foreach (var consumable in data.consumableItems)
                {
                    if (consumable != null && !string.IsNullOrEmpty(consumable.id))
                    {
                        itemsById[consumable.id] = consumable;
                        worldItemIds.Add(consumable.id);
                        registeredItems++;
                    }
                }
            }

            if (data.weaponItems != null)
            {
                foreach (var weapon in data.weaponItems)
                {
                    if (weapon != null && !string.IsNullOrEmpty(weapon.id))
                    {
                        itemsById[weapon.id] = weapon;
                        worldItemIds.Add(weapon.id);
                        registeredItems++;
                    }
                }
            }

            if (data.lootTables != null)
            {
                foreach (var lootTable in data.lootTables)
                {
                    if (lootTable != null && !string.IsNullOrEmpty(lootTable.id))
                    {
                        lootTablesById[lootTable.id] = lootTable;
                    }
                }
            }

            if (data.enemies != null)
            {
                foreach (var enemy in data.enemies)
                {
                    if (enemy != null && !string.IsNullOrEmpty(enemy.id))
                    {
                        enemiesById[enemy.id] = enemy;
                    }
                }
            }

            Debug.Log(
                $"[ServerItemsRegistry] Registered {registeredItems} items, {enemiesById.Count} enemies and {lootTablesById.Count} loot tables for world."
            );
        }

        public static bool IsWorldItem(string id)
        {
            return !string.IsNullOrEmpty(id) && itemsById.ContainsKey(id);
        }

        public static ItemData GetItemById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            itemsById.TryGetValue(id, out var item);
            return item;
        }

        public static ConsumableItemData GetConsumableById(string id)
        {
            return GetItemById(id) as ConsumableItemData;
        }

        public static WeaponItemData GetWeaponById(string id)
        {
            return GetItemById(id) as WeaponItemData;
        }

        public static EquipmentType GetItemTypeById(string id)
        {
            var item = GetItemById(id);
            if (item is ConsumableItemData)
                return EquipmentType.Consumable;
            if (item is WeaponItemData)
                return EquipmentType.Weapon;
            return EquipmentType.None;
        }

        public static EnemyData GetEnemyById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            enemiesById.TryGetValue(id, out var enemy);
            return enemy;
        }

        public static LootTableData GetLootTableById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            lootTablesById.TryGetValue(id, out var lootTable);
            return lootTable;
        }
    }
}
