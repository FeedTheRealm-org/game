using System.Collections.Generic;
using FTR.Core.Client.Enums;

/// <summary>
/// Director class to build preset sprite configurations using the builder pattern.
/// </summary>
public class SpriteConfigDirector
{
    private SpriteConfigBuilder _builder;

    public SpriteConfigDirector(SpriteConfigBuilder builder)
    {
        _builder = builder;
    }

    /* --- Build Armor --- */

    /// <summary>
    /// Builds the sprite configuration for the armor helmet.
    /// </summary>
    public List<SpriteConfig> BuildArmorHelmetSpriteConfig()
    {
        var tileY = 320;
        return _builder
            .Reset(480, 480, 0.5f, 0.2f)
            .AddTileToAllDirections(
                CharacterPartCategory.ArmorHelmet,
                0,
                tileY,
                480,
                tileY,
                480 * 2,
                tileY
            )
            .Build();
    }

    /// <summary>
    /// Builds the sprite configuration for the armor body, including arms, sleeves, and hands.
    /// </summary>
    public List<SpriteConfig> BuildArmorBodySpriteConfig()
    {
        var tileY = 160;
        var bodyConf = _builder
            .Reset(160, 160, 0.5f, 0.4f)
            .AddTileToAllDirections(
                CharacterPartCategory.ArmorBody,
                160,
                tileY,
                640,
                tileY,
                1120,
                tileY
            )
            .Build();

        var armsConf = BuildArmorArmsSpriteConfig();
        var sleevesConf = BuildArmorSleevesSpriteConfig();
        var handsConf = BuildArmorHandsSpriteConfig();

        bodyConf.AddRange(armsConf);
        bodyConf.AddRange(sleevesConf);
        bodyConf.AddRange(handsConf);

        return bodyConf;
    }

    public List<SpriteConfig> BuildArmorArmsSpriteConfig()
    {
        var tileY = 160;
        return _builder
            .Reset(80, 160, 0.5f, 0.7f)
            .AddTileToAllDirections(
                CharacterPartCategory.ArmorArmR,
                80,
                tileY,
                800,
                tileY,
                1040,
                tileY
            )
            .AddTileToAllDirections(
                CharacterPartCategory.ArmorArmL,
                320,
                tileY,
                560,
                tileY,
                1280,
                tileY
            )
            .Build();
    }

    public List<SpriteConfig> BuildArmorSleevesSpriteConfig()
    {
        var tileY = 0;
        return _builder
            .Reset(80, 80, 0.5f, 0.5f)
            .AddTileToAllDirections(
                CharacterPartCategory.ArmorSleeveR,
                80,
                tileY,
                800,
                tileY,
                1040,
                tileY
            )
            .AddTileToAllDirections(
                CharacterPartCategory.ArmorSleeveL,
                320,
                tileY,
                560,
                tileY,
                1280,
                tileY
            )
            .Build();
    }

    public List<SpriteConfig> BuildArmorHandsSpriteConfig()
    {
        var tileY = 80;
        return _builder
            .Reset(80, 80, 0.5f, 0.5f)
            .AddTileToAllDirections(
                CharacterPartCategory.ArmorHandR,
                80,
                tileY,
                560,
                tileY,
                1040,
                tileY
            )
            .AddTileToAllDirections(
                CharacterPartCategory.ArmorHandL,
                320,
                tileY,
                800,
                tileY,
                1280,
                tileY
            )
            .Build();
    }

    /// <summary>
    /// Builds the sprite configuration for the armor legs.
    /// </summary>
    public List<SpriteConfig> BuildArmorLegsSpriteConfig()
    {
        var tileY = 0;
        return _builder
            .Reset(80, 160, 0.5f, 0.6f)
            .AddTileToAllDirections(
                CharacterPartCategory.ArmorLegR,
                160,
                tileY,
                640,
                tileY,
                1120,
                tileY
            )
            .AddTileToAllDirections(
                CharacterPartCategory.ArmorLegL,
                240,
                tileY,
                720,
                tileY,
                1200,
                tileY
            )
            .Build();
    }

    /* --- Build Body --- */

    /// <summary>
    /// Builds the sprite configuration for the character's hair.
    /// </summary>
    public List<SpriteConfig> BuildHairSpriteConfig()
    {
        var tileY = 0;
        return _builder
            .Reset(480, 640, 0.5f, 0.5f)
            .AddTileToAllDirections(
                CharacterPartCategory.Hair,
                0,
                tileY,
                480,
                tileY,
                480 * 2,
                tileY
            )
            .Build();
    }

    public List<SpriteConfig> BuildBeardSpriteConfig()
    {
        var tileY = 0;
        return _builder
            .Reset(320, 320, 0.5f, 0.5f)
            .AddTile(CharacterPartCategory.Beard, FacingDirection.Front, 0, tileY)
            .AddTile(CharacterPartCategory.Beard, FacingDirection.Left, 320, tileY)
            .Build();
    }

    public List<SpriteConfig> BuildEyeBrowsSpriteConfig()
    {
        var tileY = 0;
        var frontEyebrows = _builder
            .Reset(176, 128, 0.5f, 0.5f)
            .AddTile(CharacterPartCategory.EyeBrows, FacingDirection.Front, 0, tileY)
            .Build();
        var leftEyebrows = _builder
            .Reset(138, 128, 0.5f, 0.5f)
            .AddTile(CharacterPartCategory.EyeBrows, FacingDirection.Left, 176, tileY)
            .Build();
        var rightEyebrows = _builder
            .Reset(138, 128, 0.5f, 0.5f)
            .AddTile(CharacterPartCategory.EyeBrows, FacingDirection.Right, 176, tileY)
            .Build();

        frontEyebrows.AddRange(leftEyebrows);
        frontEyebrows.AddRange(rightEyebrows);

        return frontEyebrows;
    }

    public List<SpriteConfig> BuildEyesSpriteConfig()
    {
        var tileY = 0;
        var frontEyes = _builder
            .Reset(176, 128, 0.5f, 0.5f)
            .AddTile(CharacterPartCategory.Eyes, FacingDirection.Front, 0, tileY)
            .Build();
        var leftEyes = _builder
            .Reset(138, 128, 0.5f, 0.5f)
            .AddTile(CharacterPartCategory.Eyes, FacingDirection.Left, 176, tileY)
            .Build();
        var rightEyes = _builder
            .Reset(138, 128, 0.5f, 0.5f)
            .AddTile(CharacterPartCategory.Eyes, FacingDirection.Right, 176, tileY)
            .Build();

        frontEyes.AddRange(leftEyes);
        frontEyes.AddRange(rightEyes);

        return frontEyes;
    }

    public List<SpriteConfig> BuildMouthSpriteConfig()
    {
        var tileY = 0;
        var frontMouth = _builder
            .Reset(96, 64, 0.5f, 0.5f)
            .AddTile(CharacterPartCategory.Mouth, FacingDirection.Front, 0, tileY)
            .Build();
        var leftMouth = _builder
            .Reset(64, 64, 0.5f, 0.5f)
            .AddTile(CharacterPartCategory.Mouth, FacingDirection.Left, 96, tileY)
            .Build();
        var rightMouth = _builder
            .Reset(64, 64, 0.5f, 0.5f)
            .AddTile(CharacterPartCategory.Mouth, FacingDirection.Right, 96, tileY)
            .Build();

        frontMouth.AddRange(leftMouth);
        frontMouth.AddRange(rightMouth);

        return frontMouth;
    }

    /* --- Build Accessories --- */

    public List<SpriteConfig> BuildEarringsSpriteConfig()
    {
        var tileY = 0;
        return _builder
            .Reset(128, 192, 0.2f, 0.65f)
            .AddTile(CharacterPartCategory.EarringR, FacingDirection.Front, 128, tileY)
            .AddTile(CharacterPartCategory.EarringR, FacingDirection.Back, 384, tileY)
            .AddTile(CharacterPartCategory.EarringR, FacingDirection.Right, 640, tileY)
            .AddTile(CharacterPartCategory.EarringL, FacingDirection.Front, 0, tileY)
            .AddTile(CharacterPartCategory.EarringL, FacingDirection.Back, 256, tileY)
            .AddTile(CharacterPartCategory.EarringL, FacingDirection.Left, 512, tileY)
            .Build();
    }

    public List<SpriteConfig> BuildBackSpriteConfig()
    {
        var tileY = 0;
        return _builder
            .Reset(480, 480, 0.5f, 0.5f)
            .AddTileToAllDirections(
                CharacterPartCategory.Back,
                0,
                tileY,
                480,
                tileY,
                480 * 2,
                tileY
            )
            .Build();
    }

    public List<SpriteConfig> BuildMaskSpriteConfig()
    {
        var tileY = 0;
        return _builder
            .Reset(480, 480, 0.5f, 0.2f)
            .AddTileToAllDirections(
                CharacterPartCategory.Mask,
                0,
                tileY,
                480,
                tileY,
                480 * 2,
                tileY
            )
            .Build();
    }

    /* --- Build Weapons --- */

    /// <summary>
    /// Builds the sprite configuration for weapons (right hand only, for now only primary weapon).
    /// </summary>
    public List<SpriteConfig> BuildWeaponSpriteConfig()
    {
        var tileY = 0;
        return _builder
            .Reset(0, 0, 0.5f, 0.25f)
            .AddTile(CharacterPartCategory.WeaponR, FacingDirection.Front, 0, tileY)
            .AddTile(CharacterPartCategory.WeaponR, FacingDirection.Back, 0, tileY)
            .AddTile(CharacterPartCategory.WeaponR, FacingDirection.Left, 0, tileY)
            .AddTile(CharacterPartCategory.WeaponR, FacingDirection.Right, 0, tileY)
            .Build();
    }
}
