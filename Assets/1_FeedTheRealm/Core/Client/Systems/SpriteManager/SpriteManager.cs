using System;
using System.Collections.Generic;
using FTR.Core.Client.Enums;
using UnityEngine;

[CreateAssetMenu(fileName = "SpriteManager", menuName = "Scriptable Objects/SpriteManager")]
public class SpriteManager : ScriptableObject
{
    [SerializeField]
    private API.AssetsService assetsService;

    [SerializeField]
    private API.PlayerService playerService;

    [Header("General Settings")]
    [SerializeField]
    private Logging.Logger logger;

    // Equipment
    public event Action<Texture2D> OnArmorBodyChange;
    public event Action<Texture2D> OnArmorHelmetChange;
    public event Action<Texture2D> OnArmorLegsChange;

    // Body parts
    public event Action<Texture2D> OnHairChange;
    public event Action<Texture2D> OnBeardChange;
    public event Action<Texture2D> OnEyeBrowsChange;
    public event Action<Texture2D> OnEyesChange;
    public event Action<Texture2D> OnMouthChange;

    public event Action<Texture2D> OnBackChange;
    public event Action<Texture2D> OnEarringsChange;
    public event Action<Texture2D> OnMaskChange;

    private readonly Dictionary<CharacterPartCategory, Action<Texture2D>> partChangeActions =
        new Dictionary<CharacterPartCategory, Action<Texture2D>>();

    public void OnEnable()
    {
        partChangeActions[CharacterPartCategory.ArmorBody] = (texture) =>
            OnArmorBodyChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.ArmorHelmet] = (texture) =>
            OnArmorHelmetChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.ArmorLegR] = (texture) =>
            OnArmorLegsChange?.Invoke(texture);

        partChangeActions[CharacterPartCategory.Hair] = (texture) => OnHairChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.Beard] = (texture) =>
            OnBeardChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.EyeBrows] = (texture) =>
            OnEyeBrowsChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.Eyes] = (texture) => OnEyesChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.Mouth] = (texture) =>
            OnMouthChange?.Invoke(texture);

        partChangeActions[CharacterPartCategory.Back] = (texture) => OnBackChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.EarringR] = (texture) =>
            OnEarringsChange?.Invoke(texture);
        partChangeActions[CharacterPartCategory.Mask] = (texture) => OnMaskChange?.Invoke(texture);
    }

    public void ChangeSprite(CharacterPartCategory part, Texture2D texture)
    {
        logger.Log($"SpriteManager: Changing sprite for part {part}", this, Logging.LogType.Info);
        partChangeActions[part]?.Invoke(texture);
    }

    public CharacterPartCategory GetPartCategoryFromCategoryName(string categoryName)
    {
        categoryName = categoryName.Replace(" ", "").Replace("_", "").Replace("-", "");
        if (Enum.TryParse(categoryName, true, out CharacterPartCategory part))
        {
            logger.Log(
                $"SpriteManager: Mapped category name {categoryName} to part {part}",
                this,
                Logging.LogType.Info
            );
            return part;
        }

        logger.Log(
            $"SpriteManager: Unknown category name {categoryName}",
            this,
            Logging.LogType.Warning
        );
        return CharacterPartCategory.None;
    }
}
