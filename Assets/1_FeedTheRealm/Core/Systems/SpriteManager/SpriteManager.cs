using UnityEngine;
using System;
using System.Collections.Generic;

public enum SpritePart {
    None,
    Head,
    Hair,
    FaceHair,
    EyeBack,
    Cloth,
    Armor,
    Helmet,
    FootBoot,
    FootCloth,
    Cape,
}

[CreateAssetMenu(fileName = "SpriteManager", menuName = "Scriptable Objects/SpriteManager")]
public class SpriteManager : ScriptableObject {
    [Header("General Settings")]
    [SerializeField]
    private Logging.Logger logger;

    public Action<string> onHeadChange;
    public Action<string> onHairChange;
    public Action<string> onFaceHairChange;
    public Action<string> onEyeBackChange;
    public Action<string> onClothChange;
    public Action<string> onArmorChange;
    public Action<string> onHelmetChange;
    public Action<string> onFootBootChange;
    public Action<string> onFootClothChange;
    public Action<string> onCapeChange;

    private Dictionary<SpritePart, Action<string>> partChangeActions = new Dictionary<SpritePart, Action<string>>();

    public void OnEnable() {
        partChangeActions[SpritePart.Head] = onHeadChange;
        partChangeActions[SpritePart.Hair] = onHairChange;
        partChangeActions[SpritePart.FaceHair] = onFaceHairChange;
        partChangeActions[SpritePart.EyeBack] = onEyeBackChange;
        partChangeActions[SpritePart.Cloth] = onClothChange;
        partChangeActions[SpritePart.Armor] = onArmorChange;
        partChangeActions[SpritePart.Helmet] = onHelmetChange;
        partChangeActions[SpritePart.FootBoot] = onFootBootChange;
        partChangeActions[SpritePart.FootCloth] = onFootClothChange;
        partChangeActions[SpritePart.Cape] = onCapeChange;
    }

    public void ChangeSprite(SpritePart part, string textureName) {
        partChangeActions[part]?.Invoke(textureName);
    }

    public SpritePart GetSpritePartFromCategoryName(string categoryName) {
        categoryName = categoryName.Replace(" ", "").Replace("_", "").Replace("-", "");
        if (Enum.TryParse(categoryName, true, out SpritePart part)) {
            return part;
        }

        logger.Log($"SpriteManager: Unknown category name {categoryName}", this, Logging.LogType.Warning);
        return SpritePart.None;
    }
}
