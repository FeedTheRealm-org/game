using System.Collections.Generic;
using FTR.Core.Client.Enum;

/// <summary>
/// Builder class to build configurations based on the user needs.
/// </summary>
public class SpriteConfigBuilder
{
    private List<SpriteConfig> configs = new List<SpriteConfig>();
    private float tileWidth;
    private float tileHeight;
    private float pivotX;
    private float pivotY;

    public SpriteConfigBuilder()
    {
        this.tileWidth = 0;
        this.tileHeight = 0;
        this.pivotX = 0.5f;
        this.pivotY = 0.5f;
    }

    public SpriteConfigBuilder(
        float tileWidth,
        float tileHeight,
        float pivotX = 0.5f,
        float pivotY = 0.5f
    )
    {
        this.tileWidth = tileWidth;
        this.tileHeight = tileHeight;
        this.pivotX = pivotX;
        this.pivotY = pivotY;
    }

    public SpriteConfigBuilder Reset(
        float newTileWidth,
        float newTileHeight,
        float pivotX = 0.5f,
        float pivotY = 0.5f
    )
    {
        configs.Clear();
        this.tileWidth = newTileWidth;
        this.tileHeight = newTileHeight;
        this.pivotX = pivotX;
        this.pivotY = pivotY;
        return this;
    }

    public SpriteConfigBuilder AddTile(
        CharacterPartCategory part,
        FacingDirection direction,
        int tileX,
        int tileY
    )
    {
        configs.Add(
            new SpriteConfig(part, direction, tileX, tileY, tileWidth, tileHeight, pivotX, pivotY)
        );
        return this;
    }

    public SpriteConfigBuilder AddTileToAllDirections(
        CharacterPartCategory part,
        int frontX,
        int frontY,
        int backX,
        int backY,
        int leftX,
        int leftY
    )
    {
        AddTile(part, FacingDirection.Front, frontX, frontY);
        AddTile(part, FacingDirection.Back, backX, backY);
        AddTile(part, FacingDirection.Left, leftX, leftY);
        AddTile(part, FacingDirection.Right, leftX, leftY);
        return this;
    }

    public List<SpriteConfig> Build()
    {
        return new List<SpriteConfig>(configs);
    }
}
