using UnityEngine;
using System;
using System.Collections.Generic;

public enum SpritePart {
    None,
    Hair,
    FaceHair,
    EyeBack,
    Cloth,
    Armor,
    Helmet,
    FootBoot,
    Cape,
}

[CreateAssetMenu(fileName = "SpriteManager", menuName = "Scriptable Objects/SpriteManager")]
public class SpriteManager : ScriptableObject {
    [SerializeField]
    private API.AssetsService assetsService;

    [SerializeField]
    private API.PlayerService playerService;

    [Header("General Settings")]
    [SerializeField]
    private Logging.Logger logger;

    public Action<string> onHairChange;
    public Action<string> onFaceHairChange;
    public Action<string> onEyeBackChange;
    public Action<string> onClothChange;
    public Action<string> onArmorChange;
    public Action<string> onHelmetChange;
    public Action<string> onFootBootChange;
    public Action<string> onCapeChange;

    private readonly Dictionary<SpritePart, Action<string>> partChangeActions = new Dictionary<SpritePart, Action<string>>();

    public void OnEnable() {
        partChangeActions[SpritePart.Hair] = (id) => onHairChange?.Invoke(id);
        partChangeActions[SpritePart.FaceHair] = (id) => onFaceHairChange?.Invoke(id);
        partChangeActions[SpritePart.EyeBack] = (id) => onEyeBackChange?.Invoke(id);
        partChangeActions[SpritePart.Cloth] = (id) => onClothChange?.Invoke(id);
        partChangeActions[SpritePart.Armor] = (id) => onArmorChange?.Invoke(id);
        partChangeActions[SpritePart.Helmet] = (id) => onHelmetChange?.Invoke(id);
        partChangeActions[SpritePart.FootBoot] = (id) => onFootBootChange?.Invoke(id);
        partChangeActions[SpritePart.Cape] = (id) => onCapeChange?.Invoke(id);
    }

    public void ChangeSprite(SpritePart part, string textureName) {
        logger.Log($"SpriteManager: Changing sprite for part {part} to texture {textureName}", this, Logging.LogType.Info);
        partChangeActions[part]?.Invoke(textureName);
    }

    public SpritePart GetSpritePartFromCategoryName(string categoryName) {
        categoryName = categoryName.Replace(" ", "").Replace("_", "").Replace("-", "");
        if (Enum.TryParse(categoryName, true, out SpritePart part)) {
            logger.Log($"SpriteManager: Mapped category name {categoryName} to part {part}", this, Logging.LogType.Info);
            return part;
        }

        logger.Log($"SpriteManager: Unknown category name {categoryName}", this, Logging.LogType.Warning);
        return SpritePart.None;
    }
}
