using System.Collections.Generic;
using System.Linq;
using FeedTheRealm.Gameplay.Client.SceneSetup;
using FeedTheRealm.UI.Common;
using FTR.Core.Client;
using FTR.Core.Client.EventChannels.UI;
using FTR.Core.Client.Interfaces;
using FTR.Core.Client.Settings;
using FTR.Gameplay.Client.Registry;
using FTRShared.Runtime.Core.Cache;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace FTR.UI.Homepage.Settings
{
    public class NavBarSettingsController : MonoBehaviour
    {
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private ClientPrefabProvider prefabProvider;

        [SerializeField]
        private SettingsManager settingsManager;

        [SerializeField]
        private OnLogoutRequestedEvent logoutRequestedEvent;

        [SerializeField]
        private GameObject downloadContentPopupPrefab;

        [Inject]
        private CacheManager cacheManager;

        [Inject]
        private ConfirmPopupHandle confirmPopupHandle;

        [Inject]
        private ISoundPlayer soundPlayer;

        [Inject]
        private IObjectResolver resolver;

        private IConfirmPopup ConfirmPopup => confirmPopupHandle.Controller;

        private VisualElement _root;
        private VisualElement _panel;

        private Button _displayNavButton;
        private Button _soundNavButton;
        private Button _systemNavButton;
        private Button _logoutButton;
        private Button _exitButton;

        private ScrollView _displayContent;
        private ScrollView _soundContent;
        private ScrollView _systemContent;

        private CustomDropdown _resolutionSelect;
        private Toggle _fullscreenToggle;

        private Slider _volumeSlider;
        private Slider _musicVolumeSlider;
        private Slider _sfxVolumeSlider;
        private Toggle _muteToggle;

        private Label _gameVersionLabel;
        private Button _clearCacheButton;
        private Button _downloadContentButton;
        private Toggle _cacheEnabledToggle;
        private Label _cacheStatusLabel;

        private List<Resolution> _availableResolutions = new();

        private enum Section
        {
            Display,
            Sound,
            System,
        }

        private Section _activeSection = Section.Display;

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _panel = _root.Q<VisualElement>("NavBarSettings");
            if (_panel == null)
            {
                logger.Log(
                    "[NavBarSettingsController] 'NavBarSettings' element not found in UIDocument.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }

            _displayNavButton = _panel.Q<Button>("HPSettings_DisplayButton");
            _soundNavButton = _panel.Q<Button>("HPSettings_SoundButton");
            _systemNavButton = _panel.Q<Button>("HPSettings_SystemButton");
            _logoutButton = _panel.Q<Button>("HPSettings_LogoutButton");
            _exitButton = _panel.Q<Button>("HPSettings_ExitButton");

            _displayContent = _panel.Q<ScrollView>("HPSettings_DisplayContent");
            _soundContent = _panel.Q<ScrollView>("HPSettings_SoundContent");
            _systemContent = _panel.Q<ScrollView>("HPSettings_SystemContent");

            _resolutionSelect = _panel.Q<CustomDropdown>("HPSettings_ResolutionSelect");
            _fullscreenToggle = _panel.Q<Toggle>("HPSettings_FullscreenToggle");

            _volumeSlider = _panel.Q<Slider>("HPSettings_VolumeSlider");
            _musicVolumeSlider = _panel.Q<Slider>("HPSettings_MusicVolumeSlider");
            _sfxVolumeSlider = _panel.Q<Slider>("HPSettings_SFXVolumeSlider");
            _muteToggle = _panel.Q<Toggle>("HPSettings_MuteToggle");

            _gameVersionLabel = _panel.Q<Label>("HPSettings_GameVersionLabel");
            _clearCacheButton = _panel.Q<Button>("HPSettings_ClearCacheButton");
            _downloadContentButton = _panel.Q<Button>("HPSettings_DownloadContentButton");
            _cacheEnabledToggle = _panel.Q<Toggle>("HPSettings_CacheEnabledToggle");
            _cacheStatusLabel = _panel.Q<Label>("HPSettings_CacheStatus");

            if (!ValidateElements())
                return;

            PopulateUIFromSettings();
            InitializeResolutions();
            RegisterCallbacks(register: true);
            ShowSection(Section.Display);
        }

        private void OnDisable()
        {
            RegisterCallbacks(register: false);
        }

        private bool ValidateElements()
        {
            bool ok = true;

            void Check(object el, string name)
            {
                if (el == null)
                {
                    logger.Log(
                        $"[NavBarSettingsController] Element '{name}' not found.",
                        this,
                        Logging.LogType.Error
                    );
                    ok = false;
                }
            }

            Check(_displayNavButton, "HPSettings_DisplayButton");
            Check(_soundNavButton, "HPSettings_SoundButton");
            Check(_systemNavButton, "HPSettings_SystemButton");
            Check(_logoutButton, "HPSettings_LogoutButton");
            Check(_exitButton, "HPSettings_ExitButton");
            Check(_displayContent, "HPSettings_DisplayContent");
            Check(_soundContent, "HPSettings_SoundContent");
            Check(_systemContent, "HPSettings_SystemContent");
            Check(_resolutionSelect, "HPSettings_ResolutionSelect");
            Check(_fullscreenToggle, "HPSettings_FullscreenToggle");
            Check(_volumeSlider, "HPSettings_VolumeSlider");
            Check(_musicVolumeSlider, "HPSettings_MusicVolumeSlider");
            Check(_sfxVolumeSlider, "HPSettings_SFXVolumeSlider");
            Check(_muteToggle, "HPSettings_MuteToggle");
            Check(_gameVersionLabel, "HPSettings_GameVersionLabel");
            Check(_clearCacheButton, "HPSettings_ClearCacheButton");
            Check(_downloadContentButton, "HPSettings_DownloadContentButton");
            Check(_cacheEnabledToggle, "HPSettings_CacheEnabledToggle");
            Check(_cacheStatusLabel, "HPSettings_CacheStatus");

            return ok;
        }

        private void PopulateUIFromSettings()
        {
            if (settingsManager == null)
            {
                Debug.LogError("[NavBarSettingsController] SettingsManager es NULL.");
                return;
            }

            _volumeSlider?.SetValueWithoutNotify(settingsManager.GlobalVolume);
            _musicVolumeSlider?.SetValueWithoutNotify(settingsManager.MusicVolume);
            _sfxVolumeSlider?.SetValueWithoutNotify(settingsManager.SFXVolume);
            _muteToggle?.SetValueWithoutNotify(settingsManager.IsMuted);
            _fullscreenToggle?.SetValueWithoutNotify(settingsManager.IsFullscreen);
            _cacheEnabledToggle?.SetValueWithoutNotify(settingsManager.IsCachingEnabled);
            cacheManager?.SetCachingEnabled(settingsManager.IsCachingEnabled);

            if (_gameVersionLabel != null)
                _gameVersionLabel.text = Application.version;
        }

        private void InitializeResolutions()
        {
            if (settingsManager == null)
            {
                Debug.LogError("[NavBarSettingsController] SettingsManager es NULL.");
                return;
            }

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
                $"[NavBarSettingsController] {_availableResolutions.Count} resolutions. Selected: {target}",
                this
            );
        }

        // Section switching

        private void ShowSection(Section section)
        {
            _activeSection = section;

            _displayContent.style.display =
                section == Section.Display ? DisplayStyle.Flex : DisplayStyle.None;
            _soundContent.style.display =
                section == Section.Sound ? DisplayStyle.Flex : DisplayStyle.None;
            _systemContent.style.display =
                section == Section.System ? DisplayStyle.Flex : DisplayStyle.None;

            SetNavSelected(_displayNavButton, section == Section.Display);
            SetNavSelected(_soundNavButton, section == Section.Sound);
            SetNavSelected(_systemNavButton, section == Section.System);
        }

        private static void SetNavSelected(Button btn, bool selected)
        {
            if (selected)
                btn.AddToClassList("nav-button--selected");
            else
                btn.RemoveFromClassList("nav-button--selected");
        }

        // Callbacks

        private void RegisterCallbacks(bool register)
        {
            if (register)
            {
                _displayNavButton.clicked += OnDisplayNav;
                _soundNavButton.clicked += OnSoundNav;
                _systemNavButton.clicked += OnSystemNav;
                _logoutButton.clicked += OnLogoutClicked;
                _exitButton.clicked += OnExitClicked;
                _clearCacheButton.clicked += OnClearCacheClicked;
                _downloadContentButton.clicked += OnDownloadContentClicked;
                _cacheEnabledToggle?.RegisterValueChangedCallback(OnCacheEnabledChanged);

                _fullscreenToggle?.RegisterValueChangedCallback(OnFullscreenChanged);
                _resolutionSelect.OnValueChanged += OnResolutionChanged;

                _volumeSlider?.RegisterValueChangedCallback(OnVolumeChanged);
                _musicVolumeSlider?.RegisterValueChangedCallback(OnMusicVolumeChanged);
                _sfxVolumeSlider?.RegisterValueChangedCallback(OnSFXVolumeChanged);
                _muteToggle?.RegisterValueChangedCallback(OnMuteChanged);
            }
            else
            {
                if (_displayNavButton != null)
                    _displayNavButton.clicked -= OnDisplayNav;
                if (_soundNavButton != null)
                    _soundNavButton.clicked -= OnSoundNav;
                if (_systemNavButton != null)
                    _systemNavButton.clicked -= OnSystemNav;
                if (_logoutButton != null)
                    _logoutButton.clicked -= OnLogoutClicked;
                if (_exitButton != null)
                    _exitButton.clicked -= OnExitClicked;
                if (_clearCacheButton != null)
                    _clearCacheButton.clicked -= OnClearCacheClicked;
                if (_downloadContentButton != null)
                    _downloadContentButton.clicked -= OnDownloadContentClicked;
                _cacheEnabledToggle?.UnregisterValueChangedCallback(OnCacheEnabledChanged);

                _fullscreenToggle?.UnregisterValueChangedCallback(OnFullscreenChanged);
                if (_resolutionSelect != null)
                    _resolutionSelect.OnValueChanged -= OnResolutionChanged;

                _volumeSlider?.UnregisterValueChangedCallback(OnVolumeChanged);
                _musicVolumeSlider?.UnregisterValueChangedCallback(OnMusicVolumeChanged);
                _sfxVolumeSlider?.UnregisterValueChangedCallback(OnSFXVolumeChanged);
                _muteToggle?.UnregisterValueChangedCallback(OnMuteChanged);
            }
        }

        private void OnDisplayNav()
        {
            ShowSection(Section.Display);
            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SwitchTab);
        }

        private void OnSoundNav()
        {
            ShowSection(Section.Sound);
            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SwitchTab);
        }

        private void OnSystemNav()
        {
            ShowSection(Section.System);
            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SwitchTab);
        }

        private void OnLogoutClicked()
        {
            if (logoutRequestedEvent == null)
            {
                logger.Log(
                    "[NavBarSettingsController] LogoutRequestedEvent is not assigned.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }
            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SwitchTab);

            ConfirmPopup.Show(
                question: "Are you sure you want to log out?",
                title: "Log Out",
                onConfirm: () =>
                {
                    logoutRequestedEvent.Raise();
                }
            );
        }

        private void OnExitClicked()
        {
            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SwitchTab);
            ConfirmPopup.Show(
                question: "Are you sure you want to exit the game?",
                title: "Exit Game",
                onConfirm: () =>
                {
                    Application.Quit();
                }
            );
        }

        private void OnClearCacheClicked()
        {
            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SwitchTab);
            if (cacheManager == null)
            {
                logger.Log(
                    "[NavBarSettingsController] CacheManager is not assigned.",
                    this,
                    Logging.LogType.Error
                );
                return;
            }
            ConfirmPopup.Show(
                question: "You are about to clear cached files. You will have to download them again.",
                title: "Clear Cache",
                onConfirm: () =>
                {
                    ClearCache();
                }
            );
        }

        private void OnDownloadContentClicked()
        {
            resolver.Instantiate(downloadContentPopupPrefab);
        }

        private void ClearCache()
        {
            int deletedCount = cacheManager.ClearAllCache();
            if (_cacheStatusLabel != null)
            {
                _cacheStatusLabel.text =
                    deletedCount > 0
                        ? $"Cleared cache: {deletedCount} files removed."
                        : "Cache already empty.";
            }
            logger.Log(
                $"[NavBarSettingsController] Cache cleared (files removed: {deletedCount}).",
                this
            );
        }

        private void OnCacheEnabledChanged(ChangeEvent<bool> evt)
        {
            settingsManager.IsCachingEnabled = evt.newValue;
            settingsManager.SaveSettings();
            cacheManager?.SetCachingEnabled(evt.newValue);

            if (_cacheStatusLabel != null)
            {
                _cacheStatusLabel.text = evt.newValue
                    ? "Caching enabled."
                    : "Caching disabled. Files will not be stored.";
            }
            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SwitchTab);
        }

        // Audio callbacks

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
        }

        private void OnMuteChanged(ChangeEvent<bool> evt)
        {
            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SwitchTab);
            settingsManager.IsMuted = evt.newValue;
            settingsManager.SaveSettings();
            float musicVol = evt.newValue ? 0f : settingsManager.MusicVolume;
            MusicPlayer.Instance?.SetGlobalMusicVolume(musicVol);
        }

        // Display callbacks

        private void OnFullscreenChanged(ChangeEvent<bool> evt)
        {
            settingsManager.IsFullscreen = evt.newValue;
            settingsManager.SaveSettings();
            soundPlayer.PlayUI(ClientSoundFXRegistry.SoundFXIds.SwitchTab);
        }

        private void OnResolutionChanged(int selectedIndex)
        {
            if (selectedIndex < 0 || selectedIndex >= _availableResolutions.Count)
                return;

            Resolution target = _availableResolutions[selectedIndex];
            settingsManager.ResolutionWidth = target.width;
            settingsManager.ResolutionHeight = target.height;
            settingsManager.RefreshRate = target.refreshRateRatio;
            settingsManager.SaveSettings();
        }
    }
}
