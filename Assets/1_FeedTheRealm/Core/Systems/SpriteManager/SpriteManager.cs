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
    public Action<Texture2D> OnHairChange;
    public Action<Texture2D> OnBeardChange;
    public Action<Texture2D> OnEyeBrowsChange;
    public Action<Texture2D> OnEyesChange;
    public Action<Texture2D> OnMouthChange;
    public Action<Texture2D> OnEarsChange;

    // Equipment
    public Action<Texture2D> OnArmorBodyChange;
    public Action<Texture2D> OnArmorHelmetChange;
    public Action<Texture2D> OnArmorArmsChange;
    public Action<Texture2D> OnArmorSleevesChange;
    public Action<Texture2D> OnArmorHandsChange;
    public Action<Texture2D> OnArmorLegsChange;

    public Action<Texture2D> OnBackChange;
    public Action<Texture2D> OnEarringsChange;
    public Action<Texture2D> OnMaskChange;

    private readonly Dictionary<CharacterPartCategory, Action<Texture2D>> partChangeActions = new Dictionary<CharacterPartCategory, Action<Texture2D>>();

    public void OnEnable() {
        partChangeActions[CharacterPartCategory.Hair] = (texture) => OnHairChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.Beard] = (texture) => OnBeardChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.EyeBrows] = (texture) => OnEyeBrowsChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.Eyes] = (texture) => OnEyesChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.Mouth] = (texture) => OnMouthChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.ArmorBody] = (texture) => OnArmorBodyChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.ArmorHelmet] = (texture) => OnArmorHelmetChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.ArmorArmR] = (texture) => OnArmorArmsChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.ArmorSleeveR] = (texture) => OnArmorSleevesChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.ArmorHandR] = (texture) => OnArmorHandsChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.ArmorLegR] = (texture) => OnArmorLegsChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.Back] = (texture) => OnBackChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.EarringR] = (texture) => OnEarringsChange?.Invoke(texture);
    }

    public void ChangeSprite(CharacterPartCategory part, Texture2D texture) {
        logger.Log($"SpriteManager: Changing sprite for part {part} to texture {texture.name}", this, Logging.LogType.Info);
        partChangeActions[part]?.Invoke(texture);
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
