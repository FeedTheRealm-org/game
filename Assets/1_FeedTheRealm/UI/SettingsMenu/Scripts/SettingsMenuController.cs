using System.Collections.Generic;
using System.Linq;
using FeedTheRealm.UI.Common;
using FTR.Core.Client;
using FTR.Core.Client.EventChannels.UI;
using FTR.Gameplay.Client.Registry;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VContainer;

public class SettingsMenuController : MonoBehaviour
{
    [Header("General settings")]
    [SerializeField]
    private SceneReference homeScene;

    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private PlayerInputReader playerInputReader;

    [SerializeField]
    private OnWorldLeaveEvent onWorldLeaveEvent;

    [Inject]
    private ClientPrefabProvider prefabProvider;

    [Inject]
    private ISoundPlayer soundPlayer;

    /* General settings */
    private VisualElement root;
    private Button _homeButton;
    private Button _exitButton;
    private Button _closeSettingsButton;

    // Sidebar nav buttons
    private Button _displayNavButton;
    private Button _soundNavButton;

    // Content sections
    private ScrollView _displayContent;
    private ScrollView _soundContent;

    /* Display settings */
    private CustomDropdown _resolutionSelect;
    private Toggle _fullscreenToggle;

    /* Audio settings */
    private Slider _volumeSlider;
    private Slider _musicVolumeSlider;
    private Slider _sfxVolumeSlider;
    private Toggle _muteToggle;
    private bool _isMuted;

    private List<Resolution> _availableResolutions;
    private const float baseHeight = 800f;

    private enum SettingsSection
    {
        Display,
        Sound,
    }

    private SettingsSection _activeSection = SettingsSection.Display;

    private void Start()
    {
        playerInputReader.CursorToggleEvent += ToggleSettings;
    }

    private void OnDestroy()
    {
        if (playerInputReader != null)
        {
            playerInputReader.CursorToggleEvent -= ToggleSettings;
        }
    }

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        /* General / nav buttons */
        _homeButton = root.Q<Button>("HomeButton");
        _exitButton = root.Q<Button>("ExitButton");
        _closeSettingsButton = root.Q<Button>("CloseButton");
        _displayNavButton = root.Q<Button>("DisplayButton");
        _soundNavButton = root.Q<Button>("SoundButton");

        /* Content sections */
        _displayContent = root.Q<ScrollView>("DisplayContent");
        _soundContent = root.Q<ScrollView>("SoundContent");

        if (
            _homeButton == null
            || _exitButton == null
            || _closeSettingsButton == null
            || _displayNavButton == null
            || _soundNavButton == null
            || _displayContent == null
            || _soundContent == null
        )
        {
            logger.Log(
                "One or more general settings UI elements not found in the UI Document.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        /* Display settings */
        _resolutionSelect = root.Q<CustomDropdown>("ResolutionSelect");
        _fullscreenToggle = root.Q<Toggle>("FullscreenToggle");
        if (_resolutionSelect == null || _fullscreenToggle == null)
        {
            logger.Log(
                "One or more display settings UI elements not found in the UI Document.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        /* Audio settings */
        _volumeSlider = root.Q<Slider>("VolumeSlider");
        if (_volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("GlobalVolume", 1f);
            _volumeSlider.value = savedVolume;
            AudioListener.volume = savedVolume;
        }

        _musicVolumeSlider = root.Q<Slider>("MusicVolumeSlider");
        if (_musicVolumeSlider != null)
        {
            float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            _musicVolumeSlider.value = savedMusicVolume;
            if (MusicPlayer.Instance != null)
            {
                MusicPlayer.Instance.SetGlobalMusicVolume(savedMusicVolume);
            }
        }

        _sfxVolumeSlider = root.Q<Slider>("SFXVolumeSlider");
        if (_sfxVolumeSlider != null)
        {
            float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            _sfxVolumeSlider.value = savedSFXVolume;
        }

        _muteToggle = root.Q<Toggle>("MuteToggle");
        if (_muteToggle != null)
        {
            _isMuted = PlayerPrefs.GetInt("SoundMuted", 0) == 1;
            _muteToggle.value = _isMuted;
            ApplyAudioMuteState();
        }

        logger.Log("Settings menu UI elements initialized successfully.", this);

        initializeDisplaySettings();
        adjustUIToScreenSize();
        registerButtonCallbacks(true);

        ShowSection(SettingsSection.Display);

        root.style.display = DisplayStyle.None;
    }

    private void OnDisable()
    {
        registerButtonCallbacks(false);
    }

    // ── SECTION SWITCHING ──────────────────────────────────────────────────────

    private void ShowSection(SettingsSection section)
    {
        _activeSection = section;

        _displayContent.style.display =
            section == SettingsSection.Display ? DisplayStyle.Flex : DisplayStyle.None;

        _soundContent.style.display =
            section == SettingsSection.Sound ? DisplayStyle.Flex : DisplayStyle.None;

        UpdateNavButtonSelection(_displayNavButton, section == SettingsSection.Display);
        UpdateNavButtonSelection(_soundNavButton, section == SettingsSection.Sound);

        soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SwitchTab);
    }

    private void UpdateNavButtonSelection(Button button, bool isSelected)
    {
        if (isSelected)
            button.AddToClassList("nav-button--selected");
        else
            button.RemoveFromClassList("nav-button--selected");
    }

    // ── SCALE ─────────────────────────────────────────────────────────────────

    private void adjustUIToScreenSize()
    {
        float scaleFactor = Screen.height / baseHeight;

        float homeFontSize = _homeButton.resolvedStyle.fontSize;
        float exitFontSize = _exitButton.resolvedStyle.fontSize;
        float resolutionFontSize = _resolutionSelect.resolvedStyle.fontSize;
        float fullscreenFontSize = _fullscreenToggle.resolvedStyle.fontSize;

        _homeButton.style.fontSize = new StyleLength(
            new Length(homeFontSize * scaleFactor, LengthUnit.Pixel)
        );
        _exitButton.style.fontSize = new StyleLength(
            new Length(exitFontSize * scaleFactor, LengthUnit.Pixel)
        );
        _resolutionSelect.style.fontSize = new StyleLength(
            new Length(resolutionFontSize * scaleFactor, LengthUnit.Pixel)
        );
        _fullscreenToggle.style.fontSize = new StyleLength(
            new Length(fullscreenFontSize * scaleFactor, LengthUnit.Pixel)
        );
    }

    // ── DISPLAY SETTINGS ──────────────────────────────────────────────────────

    private void initializeDisplaySettings()
    {
        _availableResolutions = Screen
            .resolutions.GroupBy(r => new { r.width, r.height })
            .Select(g => g.OrderByDescending(r => r.refreshRateRatio.value).First())
            .OrderByDescending(r => r.width)
            .ThenByDescending(r => r.height)
            .ToList();

        List<string> resolutionStrings = _availableResolutions
            .Select(r => $"{r.width}x{r.height}")
            .ToList();

        _resolutionSelect.SetChoices(resolutionStrings);

        string currentResolution =
            $"{Screen.currentResolution.width}x{Screen.currentResolution.height}";
        _resolutionSelect.Value = currentResolution;

        _fullscreenToggle.value = Screen.fullScreen;

        logger.Log(
            $"Initialized {_availableResolutions.Count} resolutions. Current: {currentResolution}",
            this
        );
    }

    // ── CALLBACKS ─────────────────────────────────────────────────────────────

    private void registerButtonCallbacks(bool register)
    {
        if (!register)
        {
            _homeButton.clicked -= onHomeButtonClicked;
            _exitButton.clicked -= onExitButtonClicked;
            _closeSettingsButton.clicked -= onCloseSettingsButtonClicked;
            _displayNavButton.clicked -= onDisplayNavButtonClicked;
            _soundNavButton.clicked -= onSoundNavButtonClicked;
            _fullscreenToggle.UnregisterValueChangedCallback(onFullscreenToggleChanged);
            if (_resolutionSelect != null)
            {
                _resolutionSelect.OnValueChanged -= onResolutionChangedIndex;
                return;
            }
            _volumeSlider?.UnregisterValueChangedCallback(onVolumeChanged);
            _musicVolumeSlider?.UnregisterValueChangedCallback(onMusicVolumeChanged);
            _sfxVolumeSlider?.UnregisterValueChangedCallback(onSFXVolumeChanged);
            _muteToggle?.UnregisterValueChangedCallback(onMuteToggleChanged);
            return;
        }

        _homeButton.clicked += onHomeButtonClicked;
        _exitButton.clicked += onExitButtonClicked;
        _closeSettingsButton.clicked += onCloseSettingsButtonClicked;
        _displayNavButton.clicked += onDisplayNavButtonClicked;
        _soundNavButton.clicked += onSoundNavButtonClicked;
        _fullscreenToggle.RegisterValueChangedCallback(onFullscreenToggleChanged);
        if (_resolutionSelect != null)
            _resolutionSelect.OnValueChanged += onResolutionChangedIndex;
        _volumeSlider?.RegisterValueChangedCallback(onVolumeChanged);
        _musicVolumeSlider?.RegisterValueChangedCallback(onMusicVolumeChanged);
        _sfxVolumeSlider?.RegisterValueChangedCallback(onSFXVolumeChanged);
        _muteToggle?.RegisterValueChangedCallback(onMuteToggleChanged);
    }

    // ── NAV BUTTON HANDLERS ───────────────────────────────────────────────────

    private void onDisplayNavButtonClicked() => ShowSection(SettingsSection.Display);

    private void onSoundNavButtonClicked() => ShowSection(SettingsSection.Sound);

    // ── PUBLIC API ────────────────────────────────────────────────────────────

    public bool IsOpen()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        return root.style.display == DisplayStyle.Flex;
    }

    public void ToggleSettings()
    {
        logger.Log("Toggle settings", this);

        var root = GetComponent<UIDocument>().rootVisualElement;
        bool willBeActive = root.style.display != DisplayStyle.Flex;
        root.style.display = willBeActive ? DisplayStyle.Flex : DisplayStyle.None;
        soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SettingsOpen);

        UnityEngine.Cursor.lockState = willBeActive ? CursorLockMode.None : CursorLockMode.Locked;
        UnityEngine.Cursor.visible = willBeActive;
    }

    // ── EXISTING ACTION HANDLERS (unchanged) ──────────────────────────────────

    private void onHomeButtonClicked()
    {
        var confirmPopup = Instantiate(prefabProvider.ConfirmPopup)
            .GetComponent<ConfirmPopupController>();
        confirmPopup.Show(
            question: "Are you sure you want to go to the home screen?",
            title: "Return to Home",
            onConfirm: () =>
            {
                onWorldLeaveEvent.Raise();
                SceneManager.LoadScene(homeScene.SceneName);
            }
        );
    }

    private void onExitButtonClicked()
    {
        var confirmPopup = Instantiate(prefabProvider.ConfirmPopup)
            .GetComponent<ConfirmPopupController>();
        confirmPopup.Show(
            question: "Are you sure you want to exit the game?",
            title: "Exit Game",
            onConfirm: () =>
            {
                onWorldLeaveEvent.Raise();
                Application.Quit();
            }
        );
    }

    private void onCloseSettingsButtonClicked()
    {
        logger.Log("Close settings button clicked", this, Logging.LogType.Info);
        ToggleSettings();
    }

    private void onVolumeChanged(ChangeEvent<float> evt)
    {
        PlayerPrefs.SetFloat("GlobalVolume", evt.newValue);
        PlayerPrefs.Save();
        if (!_isMuted)
        {
            AudioListener.volume = evt.newValue;
        }
    }

    private void onMusicVolumeChanged(ChangeEvent<float> evt)
    {
        PlayerPrefs.SetFloat("MusicVolume", evt.newValue);
        PlayerPrefs.Save();

        if (!_isMuted && MusicPlayer.Instance != null)
        {
            MusicPlayer.Instance.SetGlobalMusicVolume(evt.newValue);
        }
    }

    private void onSFXVolumeChanged(ChangeEvent<float> evt)
    {
        PlayerPrefs.SetFloat("SFXVolume", evt.newValue);
        PlayerPrefs.Save();

        if (!_isMuted)
        {
            var audioManager = FindFirstObjectByType<AudioManager>();
            if (audioManager != null)
            {
                audioManager.SetGlobalSFXVolume(evt.newValue);
            }
        }
    }

    private void onMuteToggleChanged(ChangeEvent<bool> evt)
    {
        _isMuted = evt.newValue;
        PlayerPrefs.SetInt("SoundMuted", _isMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyAudioMuteState();
    }

    private void ApplyAudioMuteState()
    {
        if (_isMuted)
        {
            AudioListener.volume = 0f;
            if (MusicPlayer.Instance != null)
            {
                MusicPlayer.Instance.SetGlobalMusicVolume(0f);
            }
            var audioManager = FindFirstObjectByType<AudioManager>();
            if (audioManager != null)
            {
                audioManager.SetGlobalSFXVolume(0f);
            }
        }
        else
        {
            AudioListener.volume =
                _volumeSlider != null
                    ? _volumeSlider.value
                    : PlayerPrefs.GetFloat("GlobalVolume", 1f);
            if (MusicPlayer.Instance != null)
            {
                MusicPlayer.Instance.SetGlobalMusicVolume(
                    _musicVolumeSlider != null
                        ? _musicVolumeSlider.value
                        : PlayerPrefs.GetFloat("MusicVolume", 1f)
                );
            }
            var audioManager = FindFirstObjectByType<AudioManager>();
            if (audioManager != null)
            {
                audioManager.SetGlobalSFXVolume(
                    _sfxVolumeSlider != null
                        ? _sfxVolumeSlider.value
                        : PlayerPrefs.GetFloat("SFXVolume", 1f)
                );
            }
        }
    }

    private void onFullscreenToggleChanged(ChangeEvent<bool> evt)
    {
        bool newValue = evt.newValue;
        logger.Log("Fullscreen toggle: " + newValue, this, Logging.LogType.Info);
        Screen.fullScreen = newValue;
    }

    private void onResolutionChangedIndex(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= _availableResolutions.Count)
            return;

        Resolution target = _availableResolutions[selectedIndex];
        string selected = $"{target.width}x{target.height}";

        logger.Log($"Resolution changed to: {selected}", this, Logging.LogType.Info);

        FullScreenMode mode = Screen.fullScreenMode;
        Screen.SetResolution(target.width, target.height, mode, target.refreshRateRatio);

        logger.Log(
            $"Resolution set to {target.width}x{target.height} @ {target.refreshRateRatio.value}Hz",
            this
        );
    }
}
