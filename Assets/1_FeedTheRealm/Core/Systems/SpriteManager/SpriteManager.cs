using UnityEngine;
using System;

public enum SpritePart {
    Hair,
}

[CreateAssetMenu(fileName = "SpriteManager", menuName = "Scriptable Objects/SpriteManager")]
public class SpriteManager : ScriptableObject {
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
}
