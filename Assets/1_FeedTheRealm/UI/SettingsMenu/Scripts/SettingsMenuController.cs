using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class SettingsMenuController : MonoBehaviour {
    [Header("Scenes")]
    [SerializeField]
    private SceneReference homeScene;

    [Header("General settings")]
    [SerializeField]
    private Logging.Logger logger;

    /* General settings */
    private Button _homeButton;
    private Button _exitButton;
    private Button _closeSettingsButton;

    /* Display settings */
    private DropdownField _resolutionSelect;
    private Toggle _fullscreenToggle;

    private List<Resolution> _availableResolutions;

    private void OnEnable() {
        var root = GetComponent<UIDocument>().rootVisualElement;

        /* General settings */
        _homeButton = root.Q<Button>("HomeButton");
        _exitButton = root.Q<Button>("ExitButton");
        _closeSettingsButton = root.Q<Button>("CloseButton");

        if (_homeButton == null || _exitButton == null || _closeSettingsButton == null) {
            logger.Log("One or more general settings UI elements not found in the UI Document.", this, Logging.LogType.Error);
            return;
        }

        /* Display settings */
        _resolutionSelect = root.Q<DropdownField>("ResolutionSelect");
        _fullscreenToggle = root.Q<Toggle>("FullscreenToggle");
        if (_resolutionSelect == null || _fullscreenToggle == null) {
            logger.Log("One or more display settings UI elements not found in the UI Document.", this, Logging.LogType.Error);
            return;
        }

        initializeDisplaySettings();
        registerButtonCallbacks(true);
    }

    private void OnDisable() {
        registerButtonCallbacks(false);
    }

    private void initializeDisplaySettings() {
        _availableResolutions = Screen.resolutions
            .GroupBy(r => new { r.width, r.height })
            .Select(g => g.OrderByDescending(r => r.refreshRateRatio.value).First())
            .OrderByDescending(r => r.width)
            .ThenByDescending(r => r.height)
            .ToList();

        List<string> resolutionStrings = _availableResolutions
            .Select(r => $"{r.width}x{r.height}")
            .ToList();

        _resolutionSelect.choices = resolutionStrings;

        // Set current resolution
        string currentResolution = $"{Screen.currentResolution.width}x{Screen.currentResolution.height}";
        _resolutionSelect.value = currentResolution;

        _fullscreenToggle.value = Screen.fullScreen;

        logger.Log($"Initialized {_availableResolutions.Count} resolutions. Current: {currentResolution}", this);
    }

    private void registerButtonCallbacks(bool register) {
        if (!register) {
            _homeButton.clicked -= onHomeButtonClicked;
            _exitButton.clicked -= onExitButtonClicked;
            _closeSettingsButton.clicked -= onCloseSettingsButtonClicked;
            _fullscreenToggle.UnregisterValueChangedCallback(onFullscreenToggleChanged);
            _resolutionSelect.UnregisterValueChangedCallback(onResolutionChanged);
            return;
        }
        _homeButton.clicked += onHomeButtonClicked;
        _exitButton.clicked += onExitButtonClicked;
        _closeSettingsButton.clicked += onCloseSettingsButtonClicked;
        _fullscreenToggle.RegisterValueChangedCallback(onFullscreenToggleChanged);
        _resolutionSelect.RegisterValueChangedCallback(onResolutionChanged);
    }

    public void ToggleSettings() {
        logger.Log("Toggle settings", this);

        bool willBeActive = !gameObject.activeSelf;

        if (willBeActive) {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            logger.Log("Cursor shown (toggle)", this);
        } else {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            logger.Log("Cursor hidden (toggle)", this);
        }

        gameObject.SetActive(willBeActive);
    }

    private void onHomeButtonClicked() {
        logger.Log("Home button clicked", this, Logging.LogType.Info);
        SceneManager.LoadScene(homeScene.SceneName);
    }

    private void onExitButtonClicked() {
        logger.Log("Exit button clicked", this, Logging.LogType.Info);
        Application.Quit();
    }

    private void onCloseSettingsButtonClicked() {
        logger.Log("Close settings button clicked", this, Logging.LogType.Info);
        ToggleSettings();
    }

    private void onFullscreenToggleChanged(ChangeEvent<bool> evt) {
        bool newValue = evt.newValue;
        logger.Log("Fullscreen toggle: " + newValue, this, Logging.LogType.Info);

        Screen.fullScreen = newValue;
    }

    private void onResolutionChanged(ChangeEvent<string> evt) {
        string selected = evt.newValue;
        logger.Log("Resolution changed to: " + selected, this, Logging.LogType.Info);

        var parts = selected.Split('x');
        if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h)) {
            // Find the matching resolution to get the correct refresh rate
            Resolution targetResolution = _availableResolutions.FirstOrDefault(r => r.width == w && r.height == h);
            if (targetResolution.width != 0) {
                FullScreenMode mode = Screen.fullScreenMode;
                Screen.SetResolution(w, h, mode, targetResolution.refreshRateRatio);
                logger.Log($"Resolution set to {w}x{h} @ {targetResolution.refreshRateRatio.value}Hz", this);
            }
        }
    }
}
