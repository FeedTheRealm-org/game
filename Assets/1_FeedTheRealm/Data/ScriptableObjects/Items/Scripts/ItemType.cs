using UnityEngine;

/// <summary>
/// Categorías de items en el juego
/// </summary>
public enum ItemType
{
    Consumable,    // Pociones, comida, buffs
    Weapon,        // Armas de combate
    Armor,         // Armadura y protección
    Material,      // Recursos para crafteo
    QuestItem,     // Items específicos de misiones
    Misc           // Otros items sin categoría específica
}

/// <summary>
/// Rareza del item (afecta color en UI y valor)
/// </summary>
public enum ItemRarity
{
    Common,        // Gris/Blanco - Items básicos
    Uncommon,      // Verde - Items poco comunes
    Rare,          // Azul - Items raros
    Epic,          // Morado - Items épicos
    Legendary      // Naranja/Dorado - Items legendarios
}

/// <summary>
/// Helper para obtener colores por rareza
/// </summary>
public static class ItemRarityColors
{
    public static Color GetColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => new Color(0.7f, 0.7f, 0.7f),      // Gris claro
            ItemRarity.Uncommon => new Color(0.12f, 0.88f, 0.29f), // Verde
            ItemRarity.Rare => new Color(0.0f, 0.44f, 0.87f),      // Azul
            ItemRarity.Epic => new Color(0.64f, 0.21f, 0.93f),     // Morado
            ItemRarity.Legendary => new Color(1.0f, 0.65f, 0.0f),  // Naranja
            _ => Color.white
        };
    }

    public static string GetColorHex(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => "#B3B3B3",
            ItemRarity.Uncommon => "#1FE04A",
            ItemRarity.Rare => "#0070DE",
            ItemRarity.Epic => "#A335EE",
            ItemRarity.Legendary => "#FF8000",
            _ => "#FFFFFF"
        };
    }
}
