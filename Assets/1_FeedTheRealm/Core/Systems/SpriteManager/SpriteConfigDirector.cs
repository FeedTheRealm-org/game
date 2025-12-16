using System.Collections.Generic;

/// <summary>
/// Director class to build preset sprite configurations using the builder pattern.
/// </summary>
public class SpriteConfigDirector {

    private SpriteConfigBuilder _builder;

    public SpriteConfigDirector(SpriteConfigBuilder builder) {
        _builder = builder;
    }

    public List<SpriteConfig> BuildArmorHelmetSpriteConfig() {
        var tileY = 0;
        return _builder.Reset(480, 480)
            .AddTileToAllDirections(CharacterPartCategory.Hair, 0, tileY, 480, tileY, 480 * 2, tileY)
            .Build();
    }

    public List<SpriteConfig> BuildArmorBodySpriteConfig() {
        var tileY = 160;
        return _builder.Reset(160, 160)
            .AddTileToAllDirections(CharacterPartCategory.ArmorBody, 160, tileY, 640, tileY, 1120, tileY)
            .Build();
    }

    public List<SpriteConfig> BuildArmorArmsSpriteConfig() {
        var tileY = 160;
        return _builder.Reset(80, 160)
            .AddTileToAllDirections(CharacterPartCategory.ArmorArmR, 80, tileY, 560, tileY, 1040, tileY)
            .AddTileToAllDirections(CharacterPartCategory.ArmorArmL, 320, tileY, 800, tileY, 1280, tileY)
            .Build();
    }

    public List<SpriteConfig> BuildArmorHandsSpriteConfig() {
        var tileY = 80;
        return _builder.Reset(80, 80)
            .AddTileToAllDirections(CharacterPartCategory.ArmorHandR, 80, tileY, 560, tileY, 1040, tileY)
            .AddTileToAllDirections(CharacterPartCategory.ArmorHandL, 320, tileY, 800, tileY, 1280, tileY)
            .Build();
    }

    public List<SpriteConfig> BuildArmorLegsSpriteConfig() {
        var tileY = 0;
        return _builder.Reset(80, 160)
            .AddTileToAllDirections(CharacterPartCategory.ArmorLegR, 160, tileY, 640, tileY, 1120, tileY)
            .AddTileToAllDirections(CharacterPartCategory.ArmorLegL, 240, tileY, 720, tileY, 1200, tileY)
            .Build();
    }
}
