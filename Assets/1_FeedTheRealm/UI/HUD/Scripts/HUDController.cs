using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages the HUD elements and their interactions.
/// </summary>
public class HUDController : MonoBehaviour {
    // Containers
    private VisualElement _characterData;
    private VisualElement _fastUseSlotsContainer;

    void Start() {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _characterData = root.Q<VisualElement>("CharacterData");
        _fastUseSlotsContainer = root.Q<VisualElement>("FastUseSlotsContainer");

        if (_characterData == null || _fastUseSlotsContainer == null) {
            Debug.LogError("CharacterData or FastUseSlotsContainer not found in the UI Document.");
            return;
        }

        registerButtonCallbacks();
    }

    /// <summary>
    /// Registers click event callbacks for buttons in the HUD.
    /// </summary>
    private void registerButtonCallbacks() {
        // Character Icon Button
        var _characterIconButton = _characterData.Q<Button>("CharacterIcon");
        _characterIconButton?.RegisterCallback<ClickEvent>(ev => {
            Debug.Log("Character Icon Clicked");
        });

        // Fast Use Slot Buttons
        var buttons = _fastUseSlotsContainer.Query<Button>().ToList();

        foreach (var button in buttons) {
            button.RegisterCallback<ClickEvent>(ev => {
                Debug.Log(button.name + " clicked");
            });
        }
    }
}
