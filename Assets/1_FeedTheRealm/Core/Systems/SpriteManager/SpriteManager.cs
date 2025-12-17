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
    public Action<string> OnHairChange;
    public Action<string> OnBeardChange;
    public Action<string> OnEyeBrowsChange;
    public Action<string> OnEyesChange;
    public Action<string> OnMouthChange;
    public Action<string> OnEarsChange;

    // Equipment
    public Action<string> OnArmorBodyChange;
    public Action<string> OnArmorHelmetChange;
    public Action<string> OnArmorArmsChange;
    public Action<string> OnArmorSleevesChange;
    public Action<string> OnArmorHandsChange;
    public Action<string> OnArmorLegsChange;

    public Action<string> OnBackChange;
    public Action<string> OnEarringsChange;
    public Action<string> OnMaskChange;

    private readonly Dictionary<CharacterPartCategory, Action<string>> partChangeActions = new Dictionary<CharacterPartCategory, Action<string>>();

    public void OnEnable() {
        partChangeActions[CharacterPartCategory.Hair] = (id) => OnHairChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.Beard] = (id) => OnBeardChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.EyeBrows] = (id) => OnEyeBrowsChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.Eyes] = (id) => OnEyesChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.Mouth] = (id) => OnMouthChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.ArmorBody] = (id) => OnArmorBodyChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.ArmorHelmet] = (id) => OnArmorHelmetChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.ArmorArmR] = (id) => OnArmorArmsChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.ArmorSleeveR] = (id) => OnArmorSleevesChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.ArmorHandR] = (id) => OnArmorHandsChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.ArmorLegR] = (id) => OnArmorLegsChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.Back] = (id) => OnBackChange?.Invoke(id);
        partChangeActions[CharacterPartCategory.EarringR] = (id) => OnEarringsChange?.Invoke(id);
    }

    public void ChangeSprite(CharacterPartCategory part, string textureName) {
        logger.Log($"SpriteManager: Changing sprite for part {part} to texture {textureName}", this, Logging.LogType.Info);
        partChangeActions[part]?.Invoke(textureName);
    }

    public CharacterPartCategory GetPartCategoryFromCategoryName(string categoryName) {
        categoryName = categoryName.Replace(" ", "").Replace("_", "").Replace("-", "");
        if (Enum.TryParse(categoryName, true, out CharacterPartCategory part)) {
            logger.Log($"SpriteManager: Mapped category name {categoryName} to part {part}", this, Logging.LogType.Info);
            return part;
        }

        logger.Log($"SpriteManager: Unknown category name {categoryName}", this, Logging.LogType.Warning);
        return CharacterPartCategory.None;
    }
}
