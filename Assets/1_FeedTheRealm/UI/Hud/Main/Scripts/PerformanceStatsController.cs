using System.Collections;
using FTR.Core.Client.Settings;
using FTR.Core.Common.EventChannels;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace FTR.UI.Hud.Main
{
    /// <summary>
    /// Handles performance stats (ping + FPS) UI updates. Attach to the same GameObject as UIDocument.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PerformanceStatsController : MonoBehaviour
    {
        [SerializeField]
        private Logging.Logger logger;
        private const float UpdateInterval = 0.5f;

        private VisualElement _statsContainer;
        private Label _pingValue;
        private Label _fpsValue;
        private Coroutine _updateRoutine;

        [Inject]
        private SettingsManager _settingsManager;

        [Inject]
        private PerformanceStatsToggleEvent performanceStatsToggleEvent;

        [Inject]
        private INetworkStats _networkStats;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _statsContainer = root.Q<VisualElement>("PerformanceStats");
            if (_statsContainer == null)
            {
                logger.Log(
                    "[PerformanceStatsController] PerformanceStats element not found in UIDocument.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            _pingValue = _statsContainer.Q<Label>("PingValue");
            if (_pingValue == null)
            {
                logger.Log(
                    "[PerformanceStatsController] PingValue element not found inside PerformanceStats.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }
            _fpsValue = _statsContainer.Q<Label>("FPSValue");
            if (_fpsValue == null)
            {
                logger.Log(
                    "[PerformanceStatsController] FPSValue element not found inside PerformanceStats.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }
            performanceStatsToggleEvent.OnRaised += TogglePerformanceStats;
            TogglePerformanceStats(_settingsManager.ShowPerformanceStats);
        }

        private void OnDestroy()
        {
            performanceStatsToggleEvent.OnRaised -= TogglePerformanceStats;
            if (_updateRoutine != null)
                StopCoroutine(_updateRoutine);
        }

        private void OnDisable()
        {
            if (_updateRoutine != null)
                StopCoroutine(_updateRoutine);
        }

        private IEnumerator UpdateStatsRoutine()
        {
            var wait = new WaitForSeconds(UpdateInterval);

            while (true)
            {
                // Mark the start of the measurement window.
                int startFrame = Time.frameCount;
                float startTime = Time.unscaledTime;

                yield return wait;

                // Average FPS over the window that just elapsed.
                float elapsed = Time.unscaledTime - startTime;
                int frames = Time.frameCount - startFrame;
                float fps = elapsed > 0f ? frames / elapsed : 0f;
                _fpsValue.text = $"{fps:F0}";

                if (_networkStats.IsConnected)
                {
                    double pingMs = _networkStats.RttMilliseconds;
                    _pingValue.text = $"{pingMs:F0} ms";
                }
                else
                {
                    _pingValue.text = "-- ms";
                }
            }
        }

        private void TogglePerformanceStats(bool show)
        {
            _statsContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

            if (show)
                _updateRoutine ??= StartCoroutine(UpdateStatsRoutine());
            else if (_updateRoutine != null)
            {
                StopCoroutine(_updateRoutine);
                _updateRoutine = null;
            }
        }
    }
}
