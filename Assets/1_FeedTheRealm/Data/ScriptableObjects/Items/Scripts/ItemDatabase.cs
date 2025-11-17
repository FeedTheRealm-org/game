using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Base de datos central de todos los items del juego.
/// Permite búsqueda rápida por ID y filtrado por tipo/rareza.
/// Crea una instancia usando: Right-click → Create → FeedTheRealm → Items → Database
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "FeedTheRealm/Items/Database", order = 1)]
public class ItemDatabase : ScriptableObject
{
    [Header("═══ Database Configuration ═══")]
    [Tooltip("Lista de todos los items disponibles en el juego")]
    [SerializeField] private List<ItemData> allItems = new List<ItemData>();
    
    [Header("═══ Runtime Cache ═══")]
    [Tooltip("Cache en memoria para búsquedas rápidas (se construye automáticamente)")]
    private Dictionary<string, ItemData> itemDictionary;
    
    [Header("═══ Debug Info ═══")]
    [SerializeField] private bool showDebugLogs = false;
    
    /// <summary>
    /// Inicializa el diccionario de búsqueda rápida
    /// Llamar al inicio del juego o cuando se modifique la lista
    /// </summary>
    public void Initialize()
    {
        itemDictionary = new Dictionary<string, ItemData>();
        int validItems = 0;
        int invalidItems = 0;
        
        foreach (var item in allItems)
        {
            if (item == null)
            {
                Debug.LogWarning("[ItemDatabase] Item null encontrado en la lista!", this);
                invalidItems++;
                continue;
            }
            
            if (!item.IsValid())
            {
                Debug.LogError($"[ItemDatabase] Item '{item.name}' tiene datos inválidos!", this);
                invalidItems++;
                continue;
            }
            
            if (string.IsNullOrEmpty(item.itemId))
            {
                Debug.LogError($"[ItemDatabase] Item '{item.name}' tiene itemId vacío!", this);
                invalidItems++;
                continue;
            }
            
            if (itemDictionary.ContainsKey(item.itemId))
            {
                Debug.LogError($"[ItemDatabase] ItemId duplicado detectado: '{item.itemId}'! " +
                              $"Assets: '{itemDictionary[item.itemId].name}' y '{item.name}'", this);
                invalidItems++;
                continue;
            }
            
            itemDictionary.Add(item.itemId, item);
            validItems++;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[ItemDatabase] Initialized with {validItems} valid items, {invalidItems} invalid items");
        }
    }
    
    /// <summary>
    /// Obtiene un item por su ID único
    /// </summary>
    /// <param name="itemId">ID del item a buscar</param>
    /// <returns>ItemData si existe, null si no se encuentra</returns>
    public ItemData GetItem(string itemId)
    {
        if (itemDictionary == null || itemDictionary.Count == 0)
        {
            Debug.LogWarning("[ItemDatabase] Database not initialized! Calling Initialize()...");
            Initialize();
        }
        
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogWarning("[ItemDatabase] GetItem called with null or empty itemId!");
            return null;
        }
        
        if (itemDictionary.TryGetValue(itemId, out ItemData item))
        {
            return item;
        }
        
        Debug.LogWarning($"[ItemDatabase] Item with ID '{itemId}' not found!");
        return null;
    }
    
    /// <summary>
    /// Obtiene todos los items de un tipo específico
    /// </summary>
    public List<ItemData> GetItemsByType(ItemType type)
    {
        if (itemDictionary == null || itemDictionary.Count == 0)
        {
            Initialize();
        }
        
        return allItems.Where(item => item != null && item.type == type).ToList();
    }
    
    /// <summary>
    /// Obtiene todos los items de una rareza específica
    /// </summary>
    public List<ItemData> GetItemsByRarity(ItemRarity rarity)
    {
        if (itemDictionary == null || itemDictionary.Count == 0)
        {
            Initialize();
        }
        
        return allItems.Where(item => item != null && item.rarity == rarity).ToList();
    }
    
    /// <summary>
    /// Obtiene todos los items que pueden ser dropeados como loot
    /// </summary>
    public List<ItemData> GetDroppableItems()
    {
        if (itemDictionary == null || itemDictionary.Count == 0)
        {
            Initialize();
        }
        
        return allItems.Where(item => item != null && item.isDroppable).ToList();
    }
    
    /// <summary>
    /// Obtiene todos los items consumibles
    /// </summary>
    public List<ItemData> GetConsumableItems()
    {
        if (itemDictionary == null || itemDictionary.Count == 0)
        {
            Initialize();
        }
        
        return allItems.Where(item => item != null && item.isConsumable).ToList();
    }
    
    /// <summary>
    /// Busca items por nombre (útil para sistemas de búsqueda en UI)
    /// </summary>
    public List<ItemData> SearchItemsByName(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            return new List<ItemData>();
        }
        
        if (itemDictionary == null || itemDictionary.Count == 0)
        {
            Initialize();
        }
        
        searchTerm = searchTerm.ToLower();
        return allItems.Where(item => 
            item != null && 
            (item.displayName.ToLower().Contains(searchTerm) || 
             item.itemId.ToLower().Contains(searchTerm))
        ).ToList();
    }
    
    /// <summary>
    /// Verifica si existe un item con el ID dado
    /// </summary>
    public bool HasItem(string itemId)
    {
        if (itemDictionary == null || itemDictionary.Count == 0)
        {
            Initialize();
        }
        
        return itemDictionary.ContainsKey(itemId);
    }
    
    /// <summary>
    /// Retorna el número total de items en la base de datos
    /// </summary>
    public int GetTotalItemCount()
    {
        return allItems.Count(item => item != null);
    }
    
    /// <summary>
    /// Retorna estadísticas de la base de datos
    /// </summary>
    public string GetDatabaseStats()
    {
        if (itemDictionary == null || itemDictionary.Count == 0)
        {
            Initialize();
        }
        
        int total = GetTotalItemCount();
        int consumables = GetItemsByType(ItemType.Consumable).Count;
        int weapons = GetItemsByType(ItemType.Weapon).Count;
        int armor = GetItemsByType(ItemType.Armor).Count;
        int materials = GetItemsByType(ItemType.Material).Count;
        int quests = GetItemsByType(ItemType.QuestItem).Count;
        int misc = GetItemsByType(ItemType.Misc).Count;
        
        return $"Total Items: {total}\n" +
               $"├─ Consumables: {consumables}\n" +
               $"├─ Weapons: {weapons}\n" +
               $"├─ Armor: {armor}\n" +
               $"├─ Materials: {materials}\n" +
               $"├─ Quest Items: {quests}\n" +
               $"└─ Misc: {misc}";
    }

#if UNITY_EDITOR
    [ContextMenu("Refresh Database")]
    private void RefreshDatabase()
    {
        Initialize();
        Debug.Log($"[ItemDatabase] Database refreshed!\n{GetDatabaseStats()}");
    }
    
    [ContextMenu("Print Database Stats")]
    private void PrintStats()
    {
        Debug.Log($"[ItemDatabase] Stats:\n{GetDatabaseStats()}");
    }
    
    [ContextMenu("Validate All Items")]
    private void ValidateAllItems()
    {
        int valid = 0;
        int invalid = 0;
        
        foreach (var item in allItems)
        {
            if (item == null)
            {
                invalid++;
                continue;
            }
            
            if (item.IsValid())
            {
                valid++;
            }
            else
            {
                invalid++;
            }
        }
        
        Debug.Log($"[ItemDatabase] Validation complete: {valid} valid, {invalid} invalid");
    }
    
    [ContextMenu("Find Duplicate IDs")]
    private void FindDuplicateIds()
    {
        var idGroups = allItems
            .Where(item => item != null && !string.IsNullOrEmpty(item.itemId))
            .GroupBy(item => item.itemId)
            .Where(group => group.Count() > 1);
        
        bool found = false;
        foreach (var group in idGroups)
        {
            found = true;
            Debug.LogError($"[ItemDatabase] Duplicate ID '{group.Key}' found in: {string.Join(", ", group.Select(i => i.name))}");
        }
        
        if (!found)
        {
            Debug.Log("[ItemDatabase] No duplicate IDs found!");
        }
    }
#endif
}
