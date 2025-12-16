using UnityEngine;

[System.Serializable]
public class SpriteConfig {

    public CharacterSpritePart Part { get; private set; }
    public FacingDirection Direction { get; private set; }
    public Rect Rect { get; private set; }
    public Vector2 Pivot { get; private set; } = new Vector2(0.5f, 0.5f);
    public float PixelsPerUnit { get; private set; } = 32f;

    public SpriteConfig(CharacterSpritePart part, FacingDirection direction, float x, float y, float width, float height) {
        this.Part = part;
        this.Direction = direction;
        this.Rect = new Rect(x, y, width, height);
    }

    public SpriteConfig(CharacterSpritePart part, FacingDirection direction, Rect rect) {
        this.Part = part;
        this.Direction = direction;
        this.Rect = rect;
    }
}
