using FTR.Core.Client.EventChannels.Status;
using FTR.Core.Common.Systems.Status;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI.Hud.Main
{
    /// <summary>
    /// Handles health UI updates for the local player's HUD.
    /// HealthView only raises HealthChangedEvent for the local player,
    /// so no netId filtering or networking logic is needed here.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HealthUIController : MonoBehaviour
    {
        [SerializeField]
        private Logging.Logger logger;

        private ProgressBar _healthBar;

        [Inject]
        private HealthChangedEvent healthChangedEvent;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var characterData = root.Q<VisualElement>("CharacterData");
            if (characterData == null)
            {
                logger.Log(
                    "[HealthController] CharacterData element not found in UIDocument.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            _healthBar = characterData.Q<ProgressBar>("HealthBar");
            if (_healthBar == null)
            {
                logger.Log(
                    "[HealthController] HealthBar element not found inside CharacterData.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            _healthBar.value = _healthBar.highValue;
        }

        private void OnEnable()
        {
            healthChangedEvent.OnRaised += OnHealthChanged;
        }

        private void OnDisable()
        {
            healthChangedEvent.OnRaised -= OnHealthChanged;
        }

        private void OnHealthChanged(HealthChangedData data)
        {
            if (_healthBar == null)
                return;

            _healthBar.value =
                data.MaxHealth > 0 ? data.CurrentHealth / data.MaxHealth * _healthBar.highValue : 0;

            if (_healthBar.value < 0)
                _healthBar.value = 0;
        }
    }
}
