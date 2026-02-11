using FTR.Core.Client.Enums;
using UnityEngine;

[System.Serializable]
public class SpriteConfig
{
    public CharacterPartCategory Part { get; private set; }
    public FacingDirection Direction { get; private set; }
    public Rect Rect { get; private set; }
    public Vector2 Pivot { get; private set; }
    public float PixelsPerUnit { get; private set; } = 100f;

    public SpriteConfig(
        CharacterPartCategory part,
        FacingDirection direction,
        float x,
        float y,
        float width,
        float height,
        float pivotX = 0.5f,
        float pivotY = 0.5f
    )
    {
        this.Part = part;
        this.Direction = direction;
        this.Rect = new Rect(x, y, width, height);
        this.Pivot = new Vector2(pivotX, pivotY);
    }

    public SpriteConfig(CharacterPartCategory part, FacingDirection direction, Rect rect)
    {
        this.Part = part;
        this.Direction = direction;
        this.Rect = rect;
    }
}
