using UnityEngine;
using UnityEngine.UIElements;

public class SettingsMenuController : MonoBehaviour {
    [Header("Ingame Input")]
    [SerializeField]
    public PlayerInputReader inputReader;

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

    private void OnAwake() {
        if (inputReader == null) {
            logger.Log("InputReader is not assigned in the inspector.", this, Logging.LogType.Error);
            return;
        }
        inputReader.CursorToggleEvent += toggleSettings;
    }

    private void OnDestroy() {
        if (inputReader != null) {
            inputReader.CursorToggleEvent -= toggleSettings;
        }
    }

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

        registerButtonCallbacks(true);
    }

    private void OnDisable() {
        registerButtonCallbacks(false);
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

    private void toggleSettings() {
        gameObject.SetActive(!gameObject.activeSelf);
        cursorToggle();
    }

    private void onHomeButtonClicked() {
        logger.Log("Home button clicked", this, Logging.LogType.Info);
    }

    private void onExitButtonClicked() {
        logger.Log("Exit button clicked", this, Logging.LogType.Info);
        Application.Quit();
    }

    private void onCloseSettingsButtonClicked() {
        logger.Log("Close settings button clicked", this, Logging.LogType.Info);
        toggleSettings();
    }

    private void onFullscreenToggleChanged(ChangeEvent<bool> evt) {
        bool newValue = evt.newValue;
        logger.Log("Fullscreen toggle: " + newValue, this, Logging.LogType.Info);

        Screen.fullScreen = newValue;
    }

    private void onResolutionChanged(ChangeEvent<string> evt) {
        string selected = evt.newValue;
        logger.Log("Resolution changed to: " + selected, this, Logging.LogType.Info);

        // var parts = selected.Split('x');
        // if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h)) {
        //     Screen.SetResolution(w, h, Screen.fullScreen);
        // }
    }

    private void cursorToggle() {
        bool shouldShowCursor = !UnityEngine.Cursor.visible;

        if (shouldShowCursor) {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            logger.Log("Cursor mostrado (toggle)", this);
        } else {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            logger.Log("Cursor oculto (toggle)", this);
        }
    }
}
