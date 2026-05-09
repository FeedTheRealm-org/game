using System.Collections;
using FTR.Core.Common.Characters;
using FTR.Core.Common.Config;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI.WorldSpace
{
    [RequireComponent(typeof(UIDocument))]
    public class WorldHealthBarUI : MonoBehaviour
    {
        [Inject]
        private Config config;

        [SerializeField]
        private Logging.Logger logger;

        private VisualElement _root;
        private ProgressBar _healthBar;
        private ICharacterHealthSource _healthSource;
        private HealthView _healthView;

        private float _maxHealth;
        private bool _maxHealthInitialized = false;
        private float _pendingHealthValue = -1f;

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

            _healthView = GetComponentInParent<HealthView>();
            if (_healthView == null)
                _healthView = GetComponent<HealthView>();

            if (_healthView != null)
            {
                if (_healthView.isMaxHealthInitialized)
                {
                    InitMaxHealth(_healthView.MaxHealth);
                }
                else
                {
                    _healthView.OnMaxHealthInitialized += OnMaxHealthReady;
                }
            }
            else
            {
                logger.Log(
                    $"[WorldHealthBarUI][{gameObject.name}] HealthView not found, using config.playerMaxHealth",
                    this,
                    Logging.LogType.Warning
                );
                InitMaxHealth(config.playerMaxHealth);
            }

            _healthSource.OnHealthChanged += OnHealthChanged;

            _root.style.display = DisplayStyle.None;
        }

        private void OnDestroy()
        {
            if (_healthSource != null)
                _healthSource.OnHealthChanged -= OnHealthChanged;

            if (_healthView != null)
                _healthView.OnMaxHealthInitialized -= OnMaxHealthReady;
        }

        private void OnMaxHealthReady(float maxHealth)
        {
            InitMaxHealth(maxHealth);
        }

        private void InitMaxHealth(float maxHealth)
        {
            _maxHealth = maxHealth;
            _maxHealthInitialized = true;

            if (_pendingHealthValue >= 0)
            {
                UpdateBar(_pendingHealthValue);
                _pendingHealthValue = -1f;
            }
            else
            {
                UpdateBar(_healthSource.Health);
            }
        }

        private void OnHealthChanged(float currentHealth)
        {
            if (_healthBar == null)
                return;

            if (!_maxHealthInitialized)
            {
                _pendingHealthValue = currentHealth;
                return;
            }

            StartCoroutine(UpdateHealthAfterDelay(currentHealth));
        }

        private IEnumerator UpdateHealthAfterDelay(float currentHealth)
        {
            if (currentHealth < _maxHealth)
                yield return new WaitForSeconds(config.HealthUpdateDelay);

            UpdateBar(currentHealth);
        }

        private void UpdateBar(float currentHealth)
        {
            _healthBar.value =
                _maxHealth > 0 ? currentHealth / _maxHealth * _healthBar.highValue : 0;

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
