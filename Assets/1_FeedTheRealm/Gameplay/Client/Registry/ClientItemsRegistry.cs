using System.Collections.Generic;
using FTRShared.Runtime.Models;
using UnityEngine;

namespace FTR.Gameplay.Client.Registry
{
    /// <summary>
    /// Static registry that exposes the current world's items (consumables, weapons, etc.),
    /// NPCs, and enemies to client-side gameplay systems (UI, Tooltips, Visuals).
    /// Strips out server-only concepts like loot tables.
    /// </summary>
    public static class ClientItemsRegistry
    {
        public static CreatablesData CurrentWorldData { get; private set; }

        private static readonly Dictionary<string, WeaponItemData> weaponsById =
            new Dictionary<string, WeaponItemData>();
        private static readonly Dictionary<string, ConsumableItemData> consumablesById =
            new Dictionary<string, ConsumableItemData>();
        private static readonly Dictionary<string, NPCData> npcsById =
            new Dictionary<string, NPCData>();
        private static readonly Dictionary<string, EnemyData> enemiesById =
            new Dictionary<string, EnemyData>();
        private static readonly HashSet<string> worldItemIds = new HashSet<string>();

        public static void RegisterWorldData(CreatablesData data)
        {
            CurrentWorldData = data;

            weaponsById.Clear();
            consumablesById.Clear();
            npcsById.Clear();
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
                        consumablesById[consumable.id] = consumable;
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
                        weaponsById[weapon.id] = weapon;
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

            if (data.npcs != null)
            {
                foreach (var npc in data.npcs)
                {
                    if (npc != null && !string.IsNullOrEmpty(npc.id))
                    {
                        npcsById[npc.id] = npc;
                    }
                }
            }

            Debug.Log(
                $"[ClientItemsRegistry] Registered {registeredItems} items, {npcsById.Count} NPC visuals and {enemiesById.Count} enemy visuals for world."
            );
        }

        public static bool IsWorldItem(string id)
        {
            return !string.IsNullOrEmpty(id)
                && (weaponsById.ContainsKey(id) || consumablesById.ContainsKey(id));
        }

        public static ItemData GetItemById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            weaponsById.TryGetValue(id, out var weapon);
            if (weapon != null)
                return weapon;
            consumablesById.TryGetValue(id, out var consumable);
            if (consumable != null)
                return consumable;

            return null;
        }

        public static ConsumableItemData GetConsumableById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            consumablesById.TryGetValue(id, out var consumable);
            return consumable;
        }

        public static ConsumableItemData GetConsumableBySpriteId(string spriteId)
        {
            return GetConsumableById(spriteId);
        }

        public static WeaponItemData GetWeaponById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            weaponsById.TryGetValue(id, out var weapon);
            return weapon;
        }

        public static EnemyData GetEnemyById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            enemiesById.TryGetValue(id, out var enemy);
            return enemy;
        }

        public static NPCData GetNpcById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            npcsById.TryGetValue(id, out var npc);
            return npc;
        }
    }
}
