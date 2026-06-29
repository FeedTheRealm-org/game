using System.Collections.Generic;
using System.Linq;
using FeedTheRealm.Gameplay.Client.SceneSetup;
using FeedTheRealm.UI.Common;
using FTR.Core.Client.EventChannels.Input;
using FTR.Core.Client.EventChannels.UI;
using FTR.Core.Client.Interfaces;
using FTR.Core.Client.Managers;
using FTR.Core.Client.Settings;
using FTR.Core.Common.EventChannels;
using FTR.Gameplay.Client.Registry;
using FTR.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VContainer;

public class SettingsMenuController : MonoBehaviour
{
    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private OnWorldLeaveEvent onWorldLeaveEvent;

    [Inject]
    private PerformanceStatsToggleEvent performanceStatsToggleEvent;

    [Inject]
    private ISoundPlayer soundPlayer;

    [Inject]
    private IAudioManager audioManager;

    [Inject]
    private SettingsManager settingsManager;

    [Inject]
    private MenuManager menuManager;

    [Inject]
    private ConfirmPopupHandle confirmPopupHandle;

    private IConfirmPopup ConfirmPopup => confirmPopupHandle.Controller;

    /* General settings */
    private VisualElement root;
    private Button _homeButton;
    private Button _exitButton;
    private Button _closeSettingsButton;
    private Button _displayNavButton;
    private Button _soundNavButton;
    private ScrollView _displayContent;
    private ScrollView _soundContent;

    /* Display */
    private CustomDropdown _resolutionSelect;
    private Toggle _fullscreenToggle;
    private Toggle _performanceStatsToggle;

    /* Audio */
    private Slider _volumeSlider;
    private Slider _musicVolumeSlider;
    private Slider _sfxVolumeSlider;
    private Toggle _muteToggle;

    private List<Resolution> _availableResolutions;
    private const float baseHeight = 800f;

    private bool _initialized = false;

    private enum SettingsSection
    {
        Display,
        Sound,
    }

    private SettingsSection _activeSection = SettingsSection.Display;

    private void Awake()
    {
        if (GetComponent<UIDocument>() == null)
            throw new MissingComponentException("UIDocument component missing.");
    }

    private void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null)
        {
            logger.Log("Root VisualElement not found.", this, Logging.LogType.Error);
            return;
        }

        /* General settings */
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
                "One or more general settings UI elements not found.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        /* Display settings */
        _resolutionSelect = root.Q<CustomDropdown>("ResolutionSelect");
        _fullscreenToggle = root.Q<Toggle>("FullscreenToggle");
        _performanceStatsToggle = root.Q<Toggle>("PerformanceStats");
        if (
            _resolutionSelect == null
            || _fullscreenToggle == null
            || _performanceStatsToggle == null
        )
        {
            logger.Log(
                "One or more display settings UI elements not found.",
                this,
                Logging.LogType.Error
            );
            return;
        }

        _volumeSlider = root.Q<Slider>("VolumeSlider");
        _musicVolumeSlider = root.Q<Slider>("MusicVolumeSlider");
        _sfxVolumeSlider = root.Q<Slider>("SFXVolumeSlider");
        _muteToggle = root.Q<Toggle>("MuteToggle");

        PopulateUIFromSettings();
        InitializeDisplayChoices();
        AdjustUIToScreenSize();
        RegisterCallbacks(register: true);
        ShowSection(SettingsSection.Display);

        menuManager.RegisterMenuCallbacks(
            MenuType.Settings,
            onOpen: OpenSettings,
            onClose: CloseSettings
        );

        root.style.display = DisplayStyle.None;

        _initialized = true;
    }

    private void OnDestroy()
    {
        if (!_initialized)
            return;
        RegisterCallbacks(register: false);
    }

    private void PopulateUIFromSettings()
    {
        _volumeSlider?.SetValueWithoutNotify(settingsManager.GlobalVolume);
        _musicVolumeSlider?.SetValueWithoutNotify(settingsManager.MusicVolume);
        _sfxVolumeSlider?.SetValueWithoutNotify(settingsManager.SFXVolume);
        _muteToggle?.SetValueWithoutNotify(settingsManager.IsMuted);
        _fullscreenToggle?.SetValueWithoutNotify(settingsManager.IsFullscreen);
        _performanceStatsToggle?.SetValueWithoutNotify(settingsManager.ShowPerformanceStats);
    }

    private void InitializeDisplayChoices()
    {
        _availableResolutions = Screen
            .resolutions.GroupBy(r => new { r.width, r.height })
            .Select(g => g.OrderByDescending(r => r.refreshRateRatio.value).First())
            .OrderByDescending(r => r.width)
            .ThenByDescending(r => r.height)
            .ToList();

        _resolutionSelect.SetChoices(
            _availableResolutions.Select(r => $"{r.width}x{r.height}").ToList()
        );

        string target =
            settingsManager.ResolutionWidth > 0
                ? $"{settingsManager.ResolutionWidth}x{settingsManager.ResolutionHeight}"
                : $"{Screen.currentResolution.width}x{Screen.currentResolution.height}";

        _resolutionSelect.Value = target;

        logger.Log(
            $"Initialized {_availableResolutions.Count} resolutions. Selected: {target}",
            this
        );
    }

    private void AdjustUIToScreenSize()
    {
        float scale = Screen.height / baseHeight;
        _homeButton.style.fontSize = Scaled(_homeButton.resolvedStyle.fontSize, scale);
        _exitButton.style.fontSize = Scaled(_exitButton.resolvedStyle.fontSize, scale);
        _resolutionSelect.style.fontSize = Scaled(_resolutionSelect.resolvedStyle.fontSize, scale);
        _fullscreenToggle.style.fontSize = Scaled(_fullscreenToggle.resolvedStyle.fontSize, scale);
    }

    private StyleLength Scaled(float size, float scale) =>
        new StyleLength(new Length(size * scale, LengthUnit.Pixel));

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

    private void RegisterCallbacks(bool register)
    {
        if (!register)
        {
            _homeButton.clicked -= OnHomeClicked;
            _exitButton.clicked -= OnExitClicked;
            _closeSettingsButton.clicked -= OnCloseClicked;
            _displayNavButton.clicked -= OnDisplayNavClicked;
            _soundNavButton.clicked -= OnSoundNavClicked;
            _fullscreenToggle.UnregisterValueChangedCallback(OnFullscreenChanged);
            _performanceStatsToggle?.UnregisterValueChangedCallback(OnPerformanceStatsChanged);
            if (_resolutionSelect != null)
                _resolutionSelect.OnValueChanged -= OnResolutionChangedIndex;
            _volumeSlider?.UnregisterValueChangedCallback(OnVolumeChanged);
            _musicVolumeSlider?.UnregisterValueChangedCallback(OnMusicVolumeChanged);
            _sfxVolumeSlider?.UnregisterValueChangedCallback(OnSFXVolumeChanged);
            _muteToggle?.UnregisterValueChangedCallback(OnMuteChanged);
            return;
        }

        _homeButton.clicked += OnHomeClicked;
        _exitButton.clicked += OnExitClicked;
        _closeSettingsButton.clicked += OnCloseClicked;
        _displayNavButton.clicked += OnDisplayNavClicked;
        _soundNavButton.clicked += OnSoundNavClicked;
        _fullscreenToggle.RegisterValueChangedCallback(OnFullscreenChanged);
        _performanceStatsToggle?.RegisterValueChangedCallback(OnPerformanceStatsChanged);
        if (_resolutionSelect != null)
            _resolutionSelect.OnValueChanged += OnResolutionChangedIndex;
        _volumeSlider?.RegisterValueChangedCallback(OnVolumeChanged);
        _musicVolumeSlider?.RegisterValueChangedCallback(OnMusicVolumeChanged);
        _sfxVolumeSlider?.RegisterValueChangedCallback(OnSFXVolumeChanged);
        _muteToggle?.RegisterValueChangedCallback(OnMuteChanged);
    }

    private void OnDisplayNavClicked() => ShowSection(SettingsSection.Display);

    private void OnSoundNavClicked() => ShowSection(SettingsSection.Sound);

    public bool IsOpen() =>
        GetComponent<UIDocument>().rootVisualElement.style.display == DisplayStyle.Flex;

    public void CloseSettings()
    {
        root.style.display = DisplayStyle.None;
        soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SettingsOpen);
        menuManager.ToggleMenu(MenuType.Settings, false);
    }

    public void OpenSettings()
    {
        root.style.display = DisplayStyle.Flex;
        soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SettingsOpen);
        menuManager.ToggleMenu(MenuType.Settings, true);
    }

    private void OnHomeClicked()
    {
        ConfirmPopup?.Show(
            question: "Are you sure you want to go to the home screen?",
            title: "Return to Home",
            onConfirm: () => onWorldLeaveEvent.Raise()
        );
    }

    private void OnExitClicked()
    {
        ConfirmPopup?.Show(
            question: "Are you sure you want to exit the game?",
            title: "Exit Game",
            onConfirm: () =>
            {
                onWorldLeaveEvent.Raise();
                Application.Quit();
            }
        );
    }

    private void OnCloseClicked()
    {
        CloseSettings();
    }

    private void OnVolumeChanged(ChangeEvent<float> evt)
    {
        settingsManager.GlobalVolume = evt.newValue;
        settingsManager.SaveSettings();
    }

    private void OnMusicVolumeChanged(ChangeEvent<float> evt)
    {
        settingsManager.MusicVolume = evt.newValue;
        settingsManager.SaveSettings();

        MusicPlayer.Instance?.SetGlobalMusicVolume(settingsManager.IsMuted ? 0f : evt.newValue);
    }

    private void OnSFXVolumeChanged(ChangeEvent<float> evt)
    {
        settingsManager.SFXVolume = evt.newValue;
        settingsManager.SaveSettings();

        audioManager.SetGlobalSFXVolume(settingsManager.IsMuted ? 0f : evt.newValue);
    }

    private void OnMuteChanged(ChangeEvent<bool> evt)
    {
        settingsManager.IsMuted = evt.newValue;
        settingsManager.SaveSettings();

        float musicVol = evt.newValue ? 0f : settingsManager.MusicVolume;
        float sfxVol = evt.newValue ? 0f : settingsManager.SFXVolume;
        MusicPlayer.Instance?.SetGlobalMusicVolume(musicVol);
        audioManager.SetGlobalSFXVolume(sfxVol);
    }

    private void OnFullscreenChanged(ChangeEvent<bool> evt)
    {
        settingsManager.IsFullscreen = evt.newValue;
        settingsManager.SaveSettings();
        logger.Log("Fullscreen: " + evt.newValue, this, Logging.LogType.Info);
    }

    private void OnPerformanceStatsChanged(ChangeEvent<bool> evt)
    {
        settingsManager.ShowPerformanceStats = evt.newValue;
        settingsManager.SaveSettings();
        performanceStatsToggleEvent.Raise(evt.newValue);
        logger.Log("Performance stats: " + evt.newValue, this, Logging.LogType.Info);
    }

    private void OnResolutionChangedIndex(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= _availableResolutions.Count)
            return;

        Resolution target = _availableResolutions[selectedIndex];
        settingsManager.ResolutionWidth = target.width;
        settingsManager.ResolutionHeight = target.height;
        settingsManager.RefreshRate = target.refreshRateRatio;
        settingsManager.SaveSettings();

        logger.Log(
            $"Resolution: {target.width}x{target.height} @ {target.refreshRateRatio.value}Hz",
            this
        );
    }
}
