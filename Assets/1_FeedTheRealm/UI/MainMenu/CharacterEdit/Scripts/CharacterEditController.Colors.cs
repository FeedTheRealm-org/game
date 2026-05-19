using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterEditController
{
    private const string ColorCategoryName = "Color";

    private const string ColorTargetSkin = "Skin";
    private const string ColorTargetHair = "Hair";
    private const string ColorTargetEyes = "Eyes";

    private VisualElement _colorControls;
    private VisualElement _paginationContainer;
    private DropdownField _colorTargetDropdown;
    private Slider _hueSlider;
    private Slider _saturationSlider;
    private Slider _valueSlider;
    private bool _isUpdatingColorControls;

    private API.CharacterColorHsv _initialSkinColor = CreateDefaultColor();
    private API.CharacterColorHsv _initialHairColor = CreateDefaultColor();
    private API.CharacterColorHsv _initialEyeColor = CreateDefaultColor();

    private void initializeColorControls()
    {
        _colorControls = _cosmeticsContainer?.Q<VisualElement>("ColorControls");
        _paginationContainer = _cosmeticsContainer?.Q<VisualElement>("Pagination");
        _colorTargetDropdown = _cosmeticsContainer?.Q<DropdownField>("ColorTargetDropdown");
        _hueSlider = _cosmeticsContainer?.Q<Slider>("HueSlider");
        _saturationSlider = _cosmeticsContainer?.Q<Slider>("SaturationSlider");
        _valueSlider = _cosmeticsContainer?.Q<Slider>("ValueSlider");

        if (_colorTargetDropdown != null)
        {
            _colorTargetDropdown.choices = new List<string>
            {
                ColorTargetSkin,
                ColorTargetHair,
                ColorTargetEyes,
            };

            if (string.IsNullOrEmpty(_colorTargetDropdown.value))
            {
                _colorTargetDropdown.SetValueWithoutNotify(ColorTargetSkin);
            }
        }

        EnsureCharacterColorFields();
        RefreshColorControlsFromRequest();
        UpdateColorControlsVisibility();
    }

    private void registerColorControlCallbacks(bool shouldRegister)
    {
        if (
            _colorTargetDropdown == null
            || _hueSlider == null
            || _saturationSlider == null
            || _valueSlider == null
        )
            return;

        if (shouldRegister)
        {
            _colorTargetDropdown.RegisterValueChangedCallback(OnColorTargetChanged);
            _hueSlider.RegisterValueChangedCallback(OnColorSliderChanged);
            _saturationSlider.RegisterValueChangedCallback(OnColorSliderChanged);
            _valueSlider.RegisterValueChangedCallback(OnColorSliderChanged);
            return;
        }

        _colorTargetDropdown.UnregisterValueChangedCallback(OnColorTargetChanged);
        _hueSlider.UnregisterValueChangedCallback(OnColorSliderChanged);
        _saturationSlider.UnregisterValueChangedCallback(OnColorSliderChanged);
        _valueSlider.UnregisterValueChangedCallback(OnColorSliderChanged);
    }

    private void OnColorTargetChanged(ChangeEvent<string> evt)
    {
        RefreshColorSlidersForSelectedTarget();
    }

    private void OnColorSliderChanged(ChangeEvent<float> evt)
    {
        if (_isUpdatingColorControls)
            return;

        ApplySlidersToCurrentColorTarget();
        ApplyAllCharacterColors();

        if (_saveButton != null)
        {
            _saveButton.text = "Save";
        }
    }

    private void ApplySlidersToCurrentColorTarget()
    {
        var color = GetCurrentTargetColor();
        color.h = Mathf.Clamp(_hueSlider.value, 0f, 360f);
        color.s = Mathf.Clamp(_saturationSlider.value, 0f, 100f);
        color.v = Mathf.Clamp(_valueSlider.value, 0f, 100f);
    }

    private void RefreshColorControlsFromRequest()
    {
        EnsureCharacterColorFields();

        if (_colorTargetDropdown != null && string.IsNullOrEmpty(_colorTargetDropdown.value))
        {
            _colorTargetDropdown.SetValueWithoutNotify(ColorTargetSkin);
        }

        RefreshColorSlidersForSelectedTarget();
    }

    private void RefreshColorSlidersForSelectedTarget()
    {
        if (_hueSlider == null || _saturationSlider == null || _valueSlider == null)
            return;

        var color = GetCurrentTargetColor();
        _isUpdatingColorControls = true;
        _hueSlider.SetValueWithoutNotify(Mathf.Clamp(color.h, 0f, 360f));
        _saturationSlider.SetValueWithoutNotify(Mathf.Clamp(color.s, 0f, 100f));
        _valueSlider.SetValueWithoutNotify(Mathf.Clamp(color.v, 0f, 100f));
        _isUpdatingColorControls = false;
    }

    private API.CharacterColorHsv GetCurrentTargetColor()
    {
        EnsureCharacterColorFields();

        var target = _colorTargetDropdown?.value ?? ColorTargetSkin;
        switch (target)
        {
            case ColorTargetHair:
                return characterInfoRequest.hair_color;
            case ColorTargetEyes:
                return characterInfoRequest.eye_color;
            default:
                return characterInfoRequest.skin_color;
        }
    }

    private static API.CharacterColorHsv CreateDefaultColor()
    {
        return new API.CharacterColorHsv
        {
            h = 0f,
            s = 0f,
            v = 100f,
        };
    }

    private static API.CharacterColorHsv CloneColorOrDefault(API.CharacterColorHsv color)
    {
        if (color == null)
            return CreateDefaultColor();

        return new API.CharacterColorHsv
        {
            h = Mathf.Clamp(color.h, 0f, 360f),
            s = Mathf.Clamp(color.s, 0f, 100f),
            v = Mathf.Clamp(color.v, 0f, 100f),
        };
    }

    private void EnsureCharacterColorFields()
    {
        if (characterInfoRequest.skin_color == null)
            characterInfoRequest.skin_color = CreateDefaultColor();
        else
        {
            characterInfoRequest.skin_color.h = Mathf.Clamp(
                characterInfoRequest.skin_color.h,
                0f,
                360f
            );
            characterInfoRequest.skin_color.s = Mathf.Clamp(
                characterInfoRequest.skin_color.s,
                0f,
                100f
            );
            characterInfoRequest.skin_color.v = Mathf.Clamp(
                characterInfoRequest.skin_color.v,
                0f,
                100f
            );
        }

        if (characterInfoRequest.hair_color == null)
            characterInfoRequest.hair_color = CreateDefaultColor();
        else
        {
            characterInfoRequest.hair_color.h = Mathf.Clamp(
                characterInfoRequest.hair_color.h,
                0f,
                360f
            );
            characterInfoRequest.hair_color.s = Mathf.Clamp(
                characterInfoRequest.hair_color.s,
                0f,
                100f
            );
            characterInfoRequest.hair_color.v = Mathf.Clamp(
                characterInfoRequest.hair_color.v,
                0f,
                100f
            );
        }

        if (characterInfoRequest.eye_color == null)
            characterInfoRequest.eye_color = CreateDefaultColor();
        else
        {
            characterInfoRequest.eye_color.h = Mathf.Clamp(
                characterInfoRequest.eye_color.h,
                0f,
                360f
            );
            characterInfoRequest.eye_color.s = Mathf.Clamp(
                characterInfoRequest.eye_color.s,
                0f,
                100f
            );
            characterInfoRequest.eye_color.v = Mathf.Clamp(
                characterInfoRequest.eye_color.v,
                0f,
                100f
            );
        }
    }

    private static Color ToUnityColor(API.CharacterColorHsv color)
    {
        if (color == null)
            return Color.white;

        var h = Mathf.Repeat(color.h, 360f) / 360f;
        var s = Mathf.Clamp01(color.s / 100f);
        var v = Mathf.Clamp01(color.v / 100f);
        return Color.HSVToRGB(h, s, v);
    }

    private void ApplyAllCharacterColors()
    {
        if (spriteManager == null)
            return;

        EnsureCharacterColorFields();
        spriteManager.ChangeSkinColor(ToUnityColor(characterInfoRequest.skin_color));
        spriteManager.ChangeHairColor(ToUnityColor(characterInfoRequest.hair_color));
        spriteManager.ChangeEyesColor(ToUnityColor(characterInfoRequest.eye_color));
    }

    private void applyCharacterColorsFromResponse(API.CharacterInfoResponse data)
    {
        characterInfoRequest.skin_color = CloneColorOrDefault(data?.skin_color);
        characterInfoRequest.hair_color = CloneColorOrDefault(data?.hair_color);
        characterInfoRequest.eye_color = CloneColorOrDefault(data?.eye_color);

        RefreshColorControlsFromRequest();
        ApplyAllCharacterColors();
    }

    private void captureInitialColorSnapshot()
    {
        _initialSkinColor = CloneColorOrDefault(characterInfoRequest.skin_color);
        _initialHairColor = CloneColorOrDefault(characterInfoRequest.hair_color);
        _initialEyeColor = CloneColorOrDefault(characterInfoRequest.eye_color);
    }

    private void resetColorsToInitialState()
    {
        characterInfoRequest.skin_color = CloneColorOrDefault(_initialSkinColor);
        characterInfoRequest.hair_color = CloneColorOrDefault(_initialHairColor);
        characterInfoRequest.eye_color = CloneColorOrDefault(_initialEyeColor);

        RefreshColorControlsFromRequest();
        ApplyAllCharacterColors();
    }

    private bool IsColorCategorySelected()
    {
        if (string.IsNullOrEmpty(_selectedCategoryName))
            return false;

        var normalized = _selectedCategoryName.Replace(" ", "").Replace("_", "").Replace("-", "");
        return string.Equals(
            normalized,
            ColorCategoryName,
            System.StringComparison.OrdinalIgnoreCase
        );
    }

    private void UpdateColorControlsVisibility()
    {
        var showColorControls = IsColorCategorySelected();

        if (_colorControls != null)
        {
            _colorControls.style.display = showColorControls
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        if (_itemsList != null)
        {
            _itemsList.style.display = showColorControls ? DisplayStyle.None : DisplayStyle.Flex;
        }

        if (_paginationContainer != null)
        {
            _paginationContainer.style.display = showColorControls
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }
    }
}
