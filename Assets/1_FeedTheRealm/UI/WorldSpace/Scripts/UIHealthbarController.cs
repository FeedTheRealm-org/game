using UnityEngine;
using UnityEngine.UIElements;

public class UIHealthbar : MonoBehaviour {
    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private HealthComponent healthComponent;

    // Containers
    private VisualElement _root;

    // Progress Bars
    private ProgressBar _healthBar;

    void Start() {
        _root = GetComponent<UIDocument>().rootVisualElement;

        _healthBar = _root.Q<ProgressBar>("WorldHealthBar");
        if (_healthBar == null) {
            logger.Log("WorldHealthBar not found in CharacterData.", this, Logging.LogType.Error);
            return;
        }

        // Initialize values
        _healthBar.value = _healthBar.highValue;
        _root.style.display = DisplayStyle.None;
    }

    private void OnEnable() {
        if (healthComponent != null) {
            healthComponent.OnHealthChanged += handleHealthChange;
        }
    }

    private void OnDisable() {
        if (healthComponent != null) {
            healthComponent.OnHealthChanged -= handleHealthChange;
        }
    }

    /// <summary>
    /// Handles changes in health and updates the HUD accordingly.
    /// </summary>
    private void handleHealthChange(float value) {
        if (_healthBar != null) {
            // Adjust for a health greater or lower than progress bar max (100).
            _healthBar.value = value * _healthBar.highValue / healthComponent.MaxHealth;
            if (value < 0) {
                _healthBar.value = 0;
            }
            toggleUIVisibility();
        }
    }

    /// <summary>
    /// Toggles the visibility of the health bar UI based on current health.
    /// Off if full health, On otherwise.
    /// </summary>
    private void toggleUIVisibility() {
        if (_root.style.display == DisplayStyle.None && _healthBar.value < _healthBar.highValue) {
            _root.style.display = DisplayStyle.Flex;
        } else if (_root.style.display == DisplayStyle.Flex && _healthBar.value == _healthBar.highValue) {
            _root.style.display = DisplayStyle.None;
        }
    }
}
