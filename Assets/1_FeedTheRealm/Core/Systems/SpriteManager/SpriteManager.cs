using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SpriteManager", menuName = "Scriptable Objects/SpriteManager")]
public class SpriteManager : ScriptableObject {
    [SerializeField]
    private API.AssetsService assetsService;

    [SerializeField]
    private API.PlayerService playerService;

    [Header("General Settings")]
    [SerializeField]
    private Logging.Logger logger;

    // Body parts
    public Action<string> onHairChange;
    public Action<string> onBeardChange;
    public Action<string> onEyeBrowsChange;
    public Action<string> onEyesChange;
    public Action<string> onMouthChange;
    public Action<string> onEarsChange;

    // Equipment
    public Action<string> onArmorChange;
    public Action<string> onBackChange;
    public Action<string> onEarringsChange;
    public Action<string> onMaskChange;

    private readonly Dictionary<CharacterPartCategory, Action<string>> partChangeActions = new Dictionary<CharacterPartCategory, Action<string>>();

    public void OnEnable() {
        partChangeActions[CharacterPartCategory.Hair] = (id) => onHairChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.Beard] = (id) => onBeardChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.EyeBrows] = (id) => onEyeBrowsChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.Eyes] = (id) => onEyesChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.Mouth] = (id) => onMouthChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.Armor] = (id) => onArmorChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.Back] = (id) => onBackChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.Earrings] = (id) => onEarringsChange?.Invoke(id);
    }

    public void ChangeSprite(CharacterPartSprite part, string textureName) {
        logger.Log($"SpriteManager: Changing sprite for part {part} to texture {textureName}", this, Logging.LogType.Info);
        partChangeActions[GetCategoryFromSpritePart(part)]?.Invoke(textureName);
    }

    public CharacterPartSprite GetSpritePartFromCategoryName(string categoryName) {
        categoryName = categoryName.Replace(" ", "").Replace("_", "").Replace("-", "");
        if (Enum.TryParse(categoryName, true, out CharacterPartSprite part)) {
            logger.Log($"SpriteManager: Mapped category name {categoryName} to part {part}", this, Logging.LogType.Info);
            return part;
        }

        logger.Log($"SpriteManager: Unknown category name {categoryName}", this, Logging.LogType.Warning);
        return CharacterPartSprite.None;
    }

    public CharacterPartCategory GetCategoryFromSpritePart(CharacterPartSprite part) {
        switch (part) {
            case CharacterPartSprite.Hair:
            case CharacterPartSprite.Beard:
            case CharacterPartSprite.EyeBrows:
            case CharacterPartSprite.Eyes:
            case CharacterPartSprite.Mouth:
                return CharacterPartCategory.Hair;
            case CharacterPartSprite.ArmorBody:
            case CharacterPartSprite.ArmorHelmet:
            case CharacterPartSprite.ArmorArmL:
            case CharacterPartSprite.ArmorSleeveL:
            case CharacterPartSprite.ArmorHandL:
            case CharacterPartSprite.ArmorArmR:
            case CharacterPartSprite.ArmorSleeveR:
            case CharacterPartSprite.ArmorHandR:
            case CharacterPartSprite.ArmorLegL:
            case CharacterPartSprite.ArmorLegR:
                return CharacterPartCategory.Armor;
            case CharacterPartSprite.EarringL:
            case CharacterPartSprite.EarringR:
                return CharacterPartCategory.Earrings;
            case CharacterPartSprite.Back:
                return CharacterPartCategory.Back;
            case CharacterPartSprite.Mask:
                return CharacterPartCategory.Mask;
            default:
                return CharacterPartCategory.None;
        }
    }
}
