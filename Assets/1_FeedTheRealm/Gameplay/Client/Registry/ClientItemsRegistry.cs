using System.Collections.Generic;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Client.Registry
{
    /// <summary>
    /// Static registry that exposes the current world's items (consumables, weapons, etc.),
    /// and enemies to client-side gameplay systems (UI, Tooltips, Visuals).
    /// Strips out server-only concepts like loot tables.
    /// </summary>
    public static class ClientItemsRegistry
    {
        public static WorldData CurrentWorldData { get; private set; }

        private static readonly Dictionary<string, ItemData> itemsById =
            new Dictionary<string, ItemData>();
        private static readonly Dictionary<string, EnemyData> enemiesById =
            new Dictionary<string, EnemyData>();
        private static readonly HashSet<string> worldItemIds = new HashSet<string>();

        public static void RegisterWorldData(WorldData data)
        {
            CurrentWorldData = data;

            itemsById.Clear();
            enemiesById.Clear();
            worldItemIds.Clear();

            if (data == null)
            {
                Debug.LogWarning("[ClientItemsRegistry] RegisterWorldData called with null data");
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
                $"[ClientItemsRegistry] Registered {registeredItems} items and {enemiesById.Count} enemies visuals for world '{data.worldName}'."
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

        public static ConsumableItemData GetConsumableBySpriteId(string spriteId)
        {
            return GetConsumableById(spriteId);
        }

        public static WeaponItemData GetWeaponById(string id)
        {
            return GetItemById(id) as WeaponItemData;
        }

        public static EnemyData GetEnemyById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            enemiesById.TryGetValue(id, out var enemy);
            return enemy;
        }
    }
}
