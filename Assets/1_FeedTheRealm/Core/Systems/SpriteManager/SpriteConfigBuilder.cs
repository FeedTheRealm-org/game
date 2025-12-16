using System.Collections.Generic;

/// <summary>
/// Builder class to build configurations based on the user needs.
/// </summary>
public class SpriteConfigBuilder {
    private List<SpriteConfig> configs = new List<SpriteConfig>();
    private float tileWidth;
    private float tileHeight;

    public SpriteConfigBuilder() {
        this.tileWidth = 0;
        this.tileHeight = 0;
    }

    public SpriteConfigBuilder(float tileWidth, float tileHeight) {
        this.tileWidth = tileWidth;
        this.tileHeight = tileHeight;
    }

    public SpriteConfigBuilder Reset(float newTileWidth, float newTileHeight) {
        configs.Clear();
        this.tileWidth = newTileWidth;
        this.tileHeight = newTileHeight;
        return this;
    }

    public SpriteConfigBuilder AddTile(CharacterSpritePart part, FacingDirection direction, int tileX, int tileY) {
        configs.Add(new SpriteConfig(
            part,
            direction,
            tileX,
            tileY,
            tileWidth,
            tileHeight
        ));
        return this;
    }

    public SpriteConfigBuilder AddTileToAllDirections(CharacterSpritePart part, int frontX, int frontY, int backX, int backY, int leftX, int leftY) {
        AddTile(part, FacingDirection.Front, frontX, frontY);
        AddTile(part, FacingDirection.Back, backX, backY);
        AddTile(part, FacingDirection.Left, leftX, leftY);
        AddTile(part, FacingDirection.Right, leftX, leftY);
        return this;
    }

    public List<SpriteConfig> Build() {
        return configs;
    }
}
