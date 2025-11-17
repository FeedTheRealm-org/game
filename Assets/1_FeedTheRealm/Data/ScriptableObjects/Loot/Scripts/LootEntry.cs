using UnityEngine;

/// <summary>
/// Representa una entrada individual en una LootTable.
/// Define un item, su probabilidad de drop y cantidad.
/// </summary>
[System.Serializable]
public class LootEntry
{
    [Header("═══ Item ═══")]
    [Tooltip("Item que puede ser dropeado")]
    public ItemData item;
    
    [Header("═══ Drop Chance ═══")]
    [Tooltip("Probabilidad de que este item sea dropeado (0-100%)")]
    [Range(0f, 100f)]
    public float dropChance = 50f;
    
    [Header("═══ Quantity ═══")]
    [Tooltip("Cantidad mínima a dropear si tiene éxito el roll")]
    [Range(1, 99)]
    public int minQuantity = 1;
    
    [Tooltip("Cantidad máxima a dropear")]
    [Range(1, 99)]
    public int maxQuantity = 1;
    
    [Header("═══ Weight (Opcional) ═══")]
    [Tooltip("Peso relativo para drops exclusivos (mayor peso = mayor chance si hay varios items con dropChance=100)")]
    [Min(1)]
    public int weight = 1;
    
    /// <summary>
    /// Valida que la entrada sea correcta
    /// </summary>
    public bool IsValid()
    {
        if (item == null)
        {
            Debug.LogWarning("[LootEntry] Item is null!");
            return false;
        }
        
        if (minQuantity > maxQuantity)
        {
            Debug.LogWarning($"[LootEntry] {item.displayName}: minQuantity ({minQuantity}) > maxQuantity ({maxQuantity})");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Intenta hacer el roll de drop
    /// </summary>
    /// <returns>Cantidad a dropear, o 0 si no tiene éxito</returns>
    public int RollDrop()
    {
        if (!IsValid()) return 0;
        
        // Roll de probabilidad
        float roll = Random.value * 100f;
        if (roll <= dropChance)
        {
            // Éxito! Determinar cantidad
            return Random.Range(minQuantity, maxQuantity + 1);
        }
        
        return 0; // No dropea
    }
}
