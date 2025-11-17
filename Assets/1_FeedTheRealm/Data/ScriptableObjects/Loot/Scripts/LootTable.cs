using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject que define una tabla de loot para un enemigo o cofre.
/// Contiene múltiples LootEntry con sus probabilidades y cantidades.
/// </summary>
[CreateAssetMenu(fileName = "New LootTable", menuName = "Feed the realm/Loot/Loot Table")]
public class LootTable : ScriptableObject
{
    [Header("═══ Información ═══")]
    [Tooltip("Nombre descriptivo de esta tabla de loot")]
    public string tableName = "New LootTable";
    
    [TextArea(2, 4)]
    [Tooltip("Descripción de qué entidad usa esta tabla")]
    public string description;
    
    [Header("═══ Entries ═══")]
    [Tooltip("Lista de items que pueden ser dropeados con sus probabilidades")]
    public List<LootEntry> lootEntries = new List<LootEntry>();
    
    [Header("═══ Drop Settings ═══")]
    [Tooltip("Probabilidad general de que se dropee algo (0-100%)")]
    [Range(0f, 100f)]
    public float overallDropChance = 100f;
    
    [Tooltip("Cantidad mínima de items diferentes a dropear")]
    [Range(0, 20)]
    public int minItemsDropped = 1;
    
    [Tooltip("Cantidad máxima de items diferentes a dropear")]
    [Range(1, 20)]
    public int maxItemsDropped = 3;
    
    [Header("═══ Advanced ═══")]
    [Tooltip("Si está activado, se garantiza al menos 1 item si el overallDropChance tiene éxito")]
    public bool guaranteeOneItem = true;
    
    [Tooltip("Si está activado, permite dropear el mismo item múltiples veces (stacking)")]
    public bool allowDuplicates = true;
    
    /// <summary>
    /// Ejecuta el roll de loot y devuelve la lista de items a dropear
    /// </summary>
    public List<ItemData> RollLoot()
    {
        List<ItemData> droppedItems = new List<ItemData>();
        
        // Roll de drop general
        float overallRoll = Random.value * 100f;
        if (overallRoll > overallDropChance)
        {
            Debug.Log($"[LootTable] {tableName}: Overall drop chance failed ({overallRoll:F1}% > {overallDropChance}%)");
            return droppedItems; // No dropea nada
        }
        
        // Validación de entries
        if (lootEntries == null || lootEntries.Count == 0)
        {
            Debug.LogWarning($"[LootTable] {tableName}: No loot entries configured!");
            return droppedItems;
        }
        
        // Determinar cuántos items diferentes vamos a intentar dropear
        int targetItemCount = Random.Range(minItemsDropped, maxItemsDropped + 1);
        
        // Crear copia de entries para trabajar
        List<LootEntry> availableEntries = new List<LootEntry>(lootEntries);
        
        // Intentar dropear items
        int attempts = 0;
        int maxAttempts = availableEntries.Count * 2; // Prevenir loops infinitos
        
        while (droppedItems.Count < targetItemCount && attempts < maxAttempts && availableEntries.Count > 0)
        {
            attempts++;
            
            // Seleccionar entry random
            int randomIndex = Random.Range(0, availableEntries.Count);
            LootEntry entry = availableEntries[randomIndex];
            
            // Roll de drop
            int quantity = entry.RollDrop();
            if (quantity > 0)
            {
                // Agregar items según la cantidad
                for (int i = 0; i < quantity; i++)
                {
                    droppedItems.Add(entry.item);
                }
                
                Debug.Log($"[LootTable] {tableName}: Dropped {quantity}x {entry.item.displayName} (chance: {entry.dropChance}%)");
            }
            
            // Si no permitimos duplicados, remover esta entry
            if (!allowDuplicates)
            {
                availableEntries.RemoveAt(randomIndex);
            }
        }
        
        // Garantizar al menos 1 item si está configurado
        if (guaranteeOneItem && droppedItems.Count == 0 && lootEntries.Count > 0)
        {
            // Forzar drop del primer item válido
            foreach (var entry in lootEntries)
            {
                if (entry.IsValid())
                {
                    int quantity = Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                    for (int i = 0; i < quantity; i++)
                    {
                        droppedItems.Add(entry.item);
                    }
                    Debug.Log($"[LootTable] {tableName}: Guaranteed drop - {quantity}x {entry.item.displayName}");
                    break;
                }
            }
        }
        
        Debug.Log($"[LootTable] {tableName}: Total drops - {droppedItems.Count} items");
        return droppedItems;
    }
    
    /// <summary>
    /// Valida la configuración de la tabla
    /// </summary>
    public bool Validate()
    {
        if (lootEntries == null || lootEntries.Count == 0)
        {
            Debug.LogError($"[LootTable] {tableName}: No loot entries configured!");
            return false;
        }
        
        bool isValid = true;
        for (int i = 0; i < lootEntries.Count; i++)
        {
            if (lootEntries[i] == null)
            {
                Debug.LogError($"[LootTable] {tableName}: Entry {i} is null!");
                isValid = false;
                continue;
            }
            
            if (!lootEntries[i].IsValid())
            {
                Debug.LogWarning($"[LootTable] {tableName}: Entry {i} is invalid!");
                isValid = false;
            }
        }
        
        if (minItemsDropped > maxItemsDropped)
        {
            Debug.LogError($"[LootTable] {tableName}: minItemsDropped ({minItemsDropped}) > maxItemsDropped ({maxItemsDropped})!");
            isValid = false;
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Editor helper: Muestra estadísticas de la tabla
    /// </summary>
    public void PrintStats()
    {
        Debug.Log($"═══════════════════════════════════════════════════");
        Debug.Log($"[LootTable] Stats for: {tableName}");
        Debug.Log($"═══════════════════════════════════════════════════");
        Debug.Log($"Overall Drop Chance: {overallDropChance}%");
        Debug.Log($"Item Count Range: {minItemsDropped} - {maxItemsDropped}");
        Debug.Log($"Guarantee One Item: {guaranteeOneItem}");
        Debug.Log($"Allow Duplicates: {allowDuplicates}");
        Debug.Log($"───────────────────────────────────────────────────");
        Debug.Log($"Entries ({lootEntries.Count}):");
        
        for (int i = 0; i < lootEntries.Count; i++)
        {
            var entry = lootEntries[i];
            if (entry == null || entry.item == null)
            {
                Debug.LogWarning($"  [{i}] NULL ENTRY");
                continue;
            }
            
            Color rarityColor = entry.item.GetRarityColor();
            string colorHex = ColorUtility.ToHtmlStringRGB(rarityColor);
            Debug.Log($"  [{i}] <color=#{colorHex}>{entry.item.displayName}</color> - {entry.dropChance}% - Qty: {entry.minQuantity}-{entry.maxQuantity}");
        }
        
        Debug.Log($"═══════════════════════════════════════════════════");
    }
    
    private void OnValidate()
    {
        // Auto-fix min/max
        if (minItemsDropped > maxItemsDropped)
        {
            maxItemsDropped = minItemsDropped;
        }
    }
}
