using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour {
    private VisualElement _characterData;
    private VisualElement _fastUseSlotsContainer;

    private Button _charcterIconButton;


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

    private void registerButtonCallbacks() {
        // Character Icon Button
        _charcterIconButton = _characterData.Q<Button>("CharacterIcon");
        _charcterIconButton?.RegisterCallback<ClickEvent>(ev => {
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
