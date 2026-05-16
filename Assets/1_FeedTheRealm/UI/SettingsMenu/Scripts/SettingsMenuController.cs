using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FeedTheRealm.UI.Common;
using FTR.Core.Client;
using FTR.Core.Client.EventChannels.UI;
using FTR.Gameplay.Client.Registry;
using Mirror;
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
    private TabView _settingsTabView;

    /* Display settings */
    private DropdownField _resolutionSelect;
    private Toggle _fullscreenToggle;

    /* Audio settings */
    private Slider _volumeSlider;
    private Slider _musicVolumeSlider;
    private Slider _sfxVolumeSlider;
    private Toggle _muteToggle;
    private bool _isMuted;

    private List<Resolution> _availableResolutions;
    private const float baseHeight = 800f;

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

        /* General settings */
        _homeButton = root.Q<Button>("HomeButton");
        _exitButton = root.Q<Button>("ExitButton");
        _closeSettingsButton = root.Q<Button>("CloseButton");
        _settingsTabView = root.Q<TabView>("SettingsTabView");

        if (_settingsTabView != null && root.Q<Tab>("SoundTab") == null)
        {
            SetupSoundTab();
        }

        if (
            _homeButton == null
            || _exitButton == null
            || _closeSettingsButton == null
            || _settingsTabView == null
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
        _resolutionSelect = root.Q<DropdownField>("ResolutionSelect");
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

        root.style.display = DisplayStyle.None;
    }

    private void OnDisable()
    {
        registerButtonCallbacks(false);
    }

    private void adjustUIToScreenSize()
    {
        float scaleFactor = Screen.height / baseHeight;

        float homeFontSize = _homeButton.resolvedStyle.fontSize;
        float exitFontSize = _exitButton.resolvedStyle.fontSize;
        float resolutionFontSize = _resolutionSelect.resolvedStyle.fontSize;
        float fullscreenFontSize = _fullscreenToggle.resolvedStyle.fontSize;
        float tabViewFontSize = _settingsTabView.resolvedStyle.fontSize;

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
        _settingsTabView.style.fontSize = new StyleLength(
            new Length(tabViewFontSize * scaleFactor, LengthUnit.Pixel)
        );
    }

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

        _resolutionSelect.choices = resolutionStrings;

        // Set current resolution
        string currentResolution =
            $"{Screen.currentResolution.width}x{Screen.currentResolution.height}";
        _resolutionSelect.value = currentResolution;

        _fullscreenToggle.value = Screen.fullScreen;

        logger.Log(
            $"Initialized {_availableResolutions.Count} resolutions. Current: {currentResolution}",
            this
        );
    }

    private void registerButtonCallbacks(bool register)
    {
        if (!register)
        {
            _homeButton.clicked -= onHomeButtonClicked;
            _exitButton.clicked -= onExitButtonClicked;
            _closeSettingsButton.clicked -= onCloseSettingsButtonClicked;
            _fullscreenToggle.UnregisterValueChangedCallback(onFullscreenToggleChanged);
            _resolutionSelect.UnregisterValueChangedCallback(onResolutionChanged);
            _volumeSlider?.UnregisterValueChangedCallback(onVolumeChanged);
            _musicVolumeSlider?.UnregisterValueChangedCallback(onMusicVolumeChanged);
            _sfxVolumeSlider?.UnregisterValueChangedCallback(onSFXVolumeChanged);
            _muteToggle?.UnregisterValueChangedCallback(onMuteToggleChanged);
            _settingsTabView?.UnregisterCallback<PointerDownEvent>(onTabClicked);
            return;
        }
        _homeButton.clicked += onHomeButtonClicked;
        _exitButton.clicked += onExitButtonClicked;
        _closeSettingsButton.clicked += onCloseSettingsButtonClicked;
        _fullscreenToggle.RegisterValueChangedCallback(onFullscreenToggleChanged);
        _resolutionSelect.RegisterValueChangedCallback(onResolutionChanged);
        _volumeSlider?.RegisterValueChangedCallback(onVolumeChanged);
        _musicVolumeSlider?.RegisterValueChangedCallback(onMusicVolumeChanged);
        _sfxVolumeSlider?.RegisterValueChangedCallback(onSFXVolumeChanged);
        _muteToggle?.RegisterValueChangedCallback(onMuteToggleChanged);
        _settingsTabView?.RegisterCallback<PointerDownEvent>(onTabClicked, TrickleDown.TrickleDown);
    }

    private void onTabClicked(PointerDownEvent evt)
    {
        var target = evt.target as VisualElement;
        if (target == null)
            return;

        bool inContent = false;
        var current = target;
        while (current != null && current != _settingsTabView)
        {
            if (current.ClassListContains("tab-content"))
            {
                inContent = true;
                break;
            }
            current = current.parent;
        }

        if (!inContent)
        {
            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SwitchTab);
        }
    }

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

    private void onResolutionChanged(ChangeEvent<string> evt)
    {
        string selected = evt.newValue;
        logger.Log("Resolution changed to: " + selected, this, Logging.LogType.Info);

        var parts = selected.Split('x');
        if (
            parts.Length == 2
            && int.TryParse(parts[0], out int w)
            && int.TryParse(parts[1], out int h)
        )
        {
            // Find the matching resolution to get the correct refresh rate
            Resolution targetResolution = _availableResolutions.FirstOrDefault(r =>
                r.width == w && r.height == h
            );
            if (targetResolution.width != 0)
            {
                FullScreenMode mode = Screen.fullScreenMode;
                Screen.SetResolution(w, h, mode, targetResolution.refreshRateRatio);
                logger.Log(
                    $"Resolution set to {w}x{h} @ {targetResolution.refreshRateRatio.value}Hz",
                    this
                );
            }
        }
    }

    private void SetupSoundTab()
    {
        var soundTab = new Tab
        {
            label = "Sound",
            name = "SoundTab",
            tabIndex = 0,
        };
        soundTab.AddToClassList("tab-button");
        soundTab.style.width = Length.Percent(100);
        soundTab.style.height = Length.Percent(100);

        var soundContent = new ScrollView { name = "SoundContent" };
        soundContent.AddToClassList("tab-content");
        soundContent.style.width = Length.Percent(100);
        soundContent.style.backgroundColor = new StyleColor(
            new Color(58f / 255f, 58f / 255f, 58f / 255f, 0.14f)
        );
        soundContent.style.alignItems = Align.FlexStart;
        soundContent.style.justifyContent = Justify.FlexStart;
        soundContent.style.paddingTop = 20;
        soundContent.style.paddingRight = 20;
        soundContent.style.paddingBottom = 20;
        soundContent.style.paddingLeft = 20;

        var volumeSlider = new Slider("Global Volume", 0f, 1f)
        {
            name = "VolumeSlider",
            value = 1f,
        };
        volumeSlider.style.width = Length.Percent(80);
        volumeSlider.style.marginBottom = 10;
        soundContent.Add(volumeSlider);

        var sfxVolumeSlider = new Slider("SFX Volume", 0f, 1f)
        {
            name = "SFXVolumeSlider",
            value = 1f,
        };
        sfxVolumeSlider.style.width = Length.Percent(80);
        sfxVolumeSlider.style.marginBottom = 10;
        soundContent.Add(sfxVolumeSlider);

        var musicVolumeSlider = new Slider("Music Volume", 0f, 1f)
        {
            name = "MusicVolumeSlider",
            value = 1f,
        };
        musicVolumeSlider.style.width = Length.Percent(80);
        musicVolumeSlider.style.marginBottom = 10;
        soundContent.Add(musicVolumeSlider);

        var muteToggle = new Toggle("Mute sound") { name = "MuteToggle", value = false };
        muteToggle.style.marginTop = 10;
        soundContent.Add(muteToggle);

        soundTab.Add(soundContent);
        _settingsTabView.Insert(1, soundTab);
    }
}
