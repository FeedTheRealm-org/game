using System.Collections;
using FTR.Core.Client.Config;
using FTR.Core.Common.Characters;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI.WorldSpace
{
    [RequireComponent(typeof(UIDocument))]
    public class WorldHealthBarUI : MonoBehaviour
    {
        [Inject]
        private ClientConfig config;

        [SerializeField]
        private Logging.Logger logger;

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

            StartCoroutine(UpdateHealthAfterDelay(currentHealth));
        }

        private IEnumerator UpdateHealthAfterDelay(float currentHealth)
        {
            if (currentHealth < config.MaxHealth)
                yield return new WaitForSeconds(config.HealthUpdateDelay); // Delay for better animation timing

            _healthBar.value =
                config.MaxHealth > 0 ? currentHealth / config.MaxHealth * _healthBar.highValue : 0;

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
