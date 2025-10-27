using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages the HUD elements and their interactions.
/// </summary>
public class HUDController : MonoBehaviour {
    [SerializeField]
    private Stamina staminaData;

    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private Session.Session session;

    // Containers
    private VisualElement _characterData;
    private VisualElement _fastUseSlotsContainer;
    private Label _nameLabel;

    // Progress Bars
    private ProgressBar _staminaBar;

    void Start() {
        var root = GetComponent<UIDocument>().rootVisualElement;

        _characterData = root.Q<VisualElement>("CharacterData");
        _fastUseSlotsContainer = root.Q<VisualElement>("FastUseSlotsContainer");
        if (_characterData == null || _fastUseSlotsContainer == null) {
            logger.Log("CharacterData or FastUseSlotsContainer not found in the UI Document.", this, Logging.LogType.Error);
            return;
        }

        _staminaBar = _characterData.Q<ProgressBar>("StaminaBar");
        if (_staminaBar == null) {
            logger.Log("StaminaBar not found in CharacterData.", this, Logging.LogType.Error);
            return;
        }

        // Initialize values
        _staminaBar.value = _staminaBar.highValue;

        _nameLabel = _characterData.Q<Label>("Username");
        if (_nameLabel != null && session != null) {
            _nameLabel.text = session.Email; // TODO: Change to character name when available
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
            logger.Log("Character Icon Clicked", this);
        });

        // Fast Use Slot Buttons
        var buttons = _fastUseSlotsContainer.Query<Button>().ToList();

        foreach (var button in buttons) {
            button.RegisterCallback<ClickEvent>(ev => {
                logger.Log(button.name + " clicked", this);
            });
        }
    }

    private void OnEnable() {
        if (staminaData != null) {
            staminaData.OnStaminaChanged += handleStaminaChange;
        }
    }

    private void OnDisable() {
        if (staminaData != null) {
            staminaData.OnStaminaChanged -= handleStaminaChange;
        }
    }

    /// <summary>
    /// Handles changes in stamina and updates the HUD accordingly.
    /// </summary>
    private void handleStaminaChange(float value) {
        if (_staminaBar != null) {
            // Adjust for a stamina greater or lower than progress bar max (100).
            _staminaBar.value = value * _staminaBar.highValue / staminaData.MaxStamina;
        }
    }
}
