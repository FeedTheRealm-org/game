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
