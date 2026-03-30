using FTR.Core.Common.Characters;
using UnityEngine;
using UnityEngine.UIElements;

namespace FTR.UI.WorldSpace
{
    [RequireComponent(typeof(UIDocument))]
    public class WorldHealthBarUI : MonoBehaviour
    {
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private float maxHealth = 100f;

        private VisualElement _root;
        private ProgressBar _healthBar;
        private ICharacterHealthSource _healthSource;

        private void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _healthBar = _root.Q<ProgressBar>("WorldHealthBar");
            if (_healthBar == null)
            {
                logger.Log("WorldHealthBar not found in UIDocument.", this, Logging.LogType.Error);
                return;
            }

            _healthSource = GetComponentInParent<ICharacterHealthSource>();
            if (_healthSource == null)
            {
                logger.Log(
                    "ICharacterHealthSource not found in parent.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            // Hide bar for local player.
            if (_healthSource.IsLocalPlayer)
            {
                _root.style.display = DisplayStyle.None;
                return;
            }

            _healthSource.OnHealthChanged += OnHealthChanged;

            // Initialise bar with current synced value, then hide until health drops.
            _healthBar.value = _healthBar.highValue;
            _root.style.display = DisplayStyle.None;
            OnHealthChanged(_healthSource.Health);
        }

        private void OnDestroy()
        {
            if (_healthSource != null)
                _healthSource.OnHealthChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(float currentHealth)
        {
            if (_healthBar == null)
                return;

            _healthBar.value = maxHealth > 0 ? currentHealth / maxHealth * _healthBar.highValue : 0;

            if (_healthBar.value < 0)
                _healthBar.value = 0;

            ToggleUIVisibility();
        }

        /// <summary>Hidden when at full health, visible otherwise.</summary>
        private void ToggleUIVisibility()
        {
            bool isFull = _healthBar.value >= _healthBar.highValue;
            _root.style.display = isFull ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
