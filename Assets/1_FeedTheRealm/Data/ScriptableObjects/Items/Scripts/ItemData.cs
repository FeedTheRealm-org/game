using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject que define las propiedades de un item en el juego.
/// Crea instancias usando: Right-click → Create → Feed the realm → Items → Item Data
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Feed the realm/Items/Item Data", order = 0)]
public class ItemData : ScriptableObject
{
    [Header("═══ Identity ═══")]
    [Tooltip("ID único del item (debe ser único en toda la base de datos). Ejemplo: 'health_potion_small'")]
    public string itemId;
    
    [Tooltip("Nombre mostrado en UI y tooltips")]
    public string displayName;
    
    [TextArea(2, 4)]
    [Tooltip("Descripción del item mostrada en tooltips")]
    public string description;
    
    [Header("═══ Visuals ═══")]
    [Tooltip("Icono mostrado en el inventario y UI")]
    public Sprite icon;
    
    [Tooltip("Prefab del item cuando aparece en el mundo (opcional - para visualización 3D)")]
    public GameObject worldPrefab;
    
    [Header("═══ Classification ═══")]
    [Tooltip("Categoría del item")]
    public ItemType type = ItemType.Misc;
    
    [Tooltip("Rareza del item (afecta color y valor)")]
    public ItemRarity rarity = ItemRarity.Common;
    
    [Header("═══ Stack & Usage ═══")]
    [Tooltip("Máximo de items por stack en el inventario")]
    [Range(1, 999)]
    public int maxStackSize = 1;
    
    [Tooltip("¿El item puede ser consumido/usado desde el inventario?")]
    public bool isConsumable = false;
    
    [Tooltip("¿El item puede ser dropeado como loot?")]
    public bool isDroppable = true;
    
    [Tooltip("¿El item puede ser descartado/eliminado por el jugador?")]
    public bool isDiscardable = true;
    
    [Tooltip("¿El item puede ser vendido a NPCs?")]
    public bool isSellable = true;
    
    [Tooltip("¿El item puede ser comerciado entre jugadores?")]
    public bool isTradeable = true;
    
    [Header("═══ Economy ═══")]
    [Tooltip("Precio de compra en tiendas (0 = no se puede comprar)")]
    [Min(0)]
    public int buyPrice = 0;
    
    [Tooltip("Precio de venta a NPCs (típicamente 25-50% del precio de compra)")]
    [Min(0)]
    public int sellPrice = 0;
    
    [Header("═══ Weight & Restrictions ═══")]
    [Tooltip("Peso del item (para sistemas de carga futuros)")]
    [Min(0f)]
    public float weight = 0f;
    
    [Tooltip("Nivel mínimo requerido para usar el item (0 = sin restricción)")]
    [Min(0)]
    public int requiredLevel = 0;
    
    // ═══ Stats & Effects (Futuro) ═══
    // TODO: Implementar sistema de efectos para consumibles
    // public List<ItemEffect> effects;
    
    // TODO: Implementar stats de armas
    // public WeaponStats weaponStats;
    
    // TODO: Implementar stats de armadura
    // public ArmorStats armorStats;
    
    /// <summary>
    /// Valida que el item tenga todos los datos necesarios
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogError($"Item '{name}' no tiene itemId asignado!", this);
            return false;
        }
        
        if (icon == null)
        {
            Debug.LogWarning($"Item '{itemId}' no tiene sprite asignado!", this);
        }
        
        if (string.IsNullOrEmpty(displayName))
        {
            Debug.LogWarning($"Item '{itemId}' no tiene displayName!", this);
        }
        
        return true;
    }
    
    /// <summary>
    /// Retorna el color asociado a la rareza del item
    /// </summary>
    public Color GetRarityColor()
    {
        return ItemRarityColors.GetColor(rarity);
    }
    
    /// <summary>
    /// Retorna el color en formato hex para UI
    /// </summary>
    public string GetRarityColorHex()
    {
        return ItemRarityColors.GetColorHex(rarity);
    }
    
    /// <summary>
    /// Retorna el nombre con color de rareza para rich text
    /// </summary>
    public string GetColoredDisplayName()
    {
        return $"<color={GetRarityColorHex()}>{displayName}</color>";
    }

#if UNITY_EDITOR
    /// <summary>
    /// Validación automática al guardar en el editor
    /// </summary>
    private void OnValidate()
    {
        // Auto-generar itemId si está vacío (basado en el nombre del asset)
        if (string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(name))
        {
            itemId = name.ToLower().Replace(" ", "_");
        }
        
        // Validar que sellPrice no sea mayor que buyPrice
        if (sellPrice > buyPrice && buyPrice > 0)
        {
            Debug.LogWarning($"Item '{itemId}': sellPrice ({sellPrice}) es mayor que buyPrice ({buyPrice})!", this);
        }
        
        // Validar stack size para quest items
        if (type == ItemType.QuestItem && maxStackSize != 1)
        {
            Debug.LogWarning($"Quest item '{itemId}' debería tener maxStackSize = 1", this);
        }
    }
#endif
}
