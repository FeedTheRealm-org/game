using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Handles stamina UI updates. Attach to the same GameObject as UIDocument.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class StaminaController : MonoBehaviour
{
    private ProgressBar _staminaBar;

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        var characterData = root.Q<VisualElement>("CharacterData");
        if (characterData == null)
            throw new Exception(
                "[StaminaController] CharacterData element not found in UIDocument."
            );

        _staminaBar = characterData.Q<ProgressBar>("StaminaBar");
        if (_staminaBar == null)
            throw new Exception(
                "[StaminaController] StaminaBar element not found inside CharacterData."
            );

        _staminaBar.value = _staminaBar.highValue;
    }

    private void OnEnable()
    {
        StaminaView.StaminaChangedEvent += OnStaminaChanged;
    }

    private void OnDisable()
    {
        StaminaView.StaminaChangedEvent -= OnStaminaChanged;
    }

    private void OnStaminaChanged(float current)
    {
        if (_staminaBar != null)
            _staminaBar.value = current;
    }
}
