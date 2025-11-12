using UnityEngine;
using System;

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

    public Action<string> onHairChange;

    public void ChangeSprite(SpritePart part, string textureName) {
        switch (part) {
            case SpritePart.Hair:
                onHairChange?.Invoke(textureName);
                break;
            default:
                Debug.LogWarning($"SpriteManager: Unknown SpritePart {part}");
                break;
        }
    }

    public SpritePart GetSpritePartFromCategoryName(string categoryName) {
        SpritePart part = SpritePart.None;
        switch (categoryName.ToLower()) {
            case "hair":
                part = SpritePart.Hair;
                break;
            default:
                logger.Log($"SpriteManager: Unknown category name {categoryName}", this, Logging.LogType.Warning);
                break;
        }
        return part;
    }
}
