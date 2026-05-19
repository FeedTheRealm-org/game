using System.Collections.Generic;
using System.Linq;
using FeedTheRealm.UI.Common;
using FTR.Core.Client.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace FTR.UI.Homepage.Settings
{
    public class NavBarSettingsController : MonoBehaviour
    {
        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private SettingsManager settingsManager;

        private VisualElement _root;
        private VisualElement _panel;

        private Button _displayNavButton;
        private Button _soundNavButton;
        private Button _exitButton;
        private Button _closeButton;

        private ScrollView _displayContent;
        private ScrollView _soundContent;

        private CustomDropdown _resolutionSelect;
        private Toggle _fullscreenToggle;

        private Slider _volumeSlider;
        private Slider _musicVolumeSlider;
        private Slider _sfxVolumeSlider;
        private Toggle _muteToggle;

        private List<Resolution> _availableResolutions = new();

        private enum Section
        {
            Display,
            Sound,
        }

        private Section _activeSection = Section.Display;

        private bool _isVisible = false;

        private void Awake()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
        }

        private void OnEnable()
        {
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

            // Sidebar buttons
            _displayNavButton = _panel.Q<Button>("HPSettings_DisplayButton");
            _soundNavButton = _panel.Q<Button>("HPSettings_SoundButton");
            _exitButton = _panel.Q<Button>("HPSettings_ExitButton");
            _closeButton = _panel.Q<Button>("HPSettings_CloseButton");

            // Content
            _displayContent = _panel.Q<ScrollView>("HPSettings_DisplayContent");
            _soundContent = _panel.Q<ScrollView>("HPSettings_SoundContent");

            // Display controls
            _resolutionSelect = _panel.Q<CustomDropdown>("HPSettings_ResolutionSelect");
            _fullscreenToggle = _panel.Q<Toggle>("HPSettings_FullscreenToggle");

            // Sound controls
            _volumeSlider = _panel.Q<Slider>("HPSettings_VolumeSlider");
            _musicVolumeSlider = _panel.Q<Slider>("HPSettings_MusicVolumeSlider");
            _sfxVolumeSlider = _panel.Q<Slider>("HPSettings_SFXVolumeSlider");
            _muteToggle = _panel.Q<Toggle>("HPSettings_MuteToggle");

            if (!ValidateElements())
                return;

            PopulateUIFromSettings();
            InitializeResolutions();
            RegisterCallbacks(register: true);
            ShowSection(Section.Display);

            Hide();
        }

        private void OnDisable()
        {
            RegisterCallbacks(register: false);
        }

        public bool IsVisible => _isVisible;

        public void Show()
        {
            if (_panel == null)
                return;
            _panel.style.display = DisplayStyle.Flex;
            _isVisible = true;
            PopulateUIFromSettings();
        }

        public void Hide()
        {
            if (_panel == null)
                return;
            _panel.style.display = DisplayStyle.None;
            _isVisible = false;
        }

        public void Toggle()
        {
            if (_isVisible)
                Hide();
            else
                Show();
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
            Check(_exitButton, "HPSettings_ExitButton");
            Check(_closeButton, "HPSettings_CloseButton");
            Check(_displayContent, "HPSettings_DisplayContent");
            Check(_soundContent, "HPSettings_SoundContent");
            Check(_resolutionSelect, "HPSettings_ResolutionSelect");
            Check(_fullscreenToggle, "HPSettings_FullscreenToggle");
            Check(_volumeSlider, "HPSettings_VolumeSlider");
            Check(_musicVolumeSlider, "HPSettings_MusicVolumeSlider");
            Check(_sfxVolumeSlider, "HPSettings_SFXVolumeSlider");
            Check(_muteToggle, "HPSettings_MuteToggle");

            return ok;
        }

        private void PopulateUIFromSettings()
        {
            _volumeSlider?.SetValueWithoutNotify(settingsManager.GlobalVolume);
            _musicVolumeSlider?.SetValueWithoutNotify(settingsManager.MusicVolume);
            _sfxVolumeSlider?.SetValueWithoutNotify(settingsManager.SFXVolume);
            _muteToggle?.SetValueWithoutNotify(settingsManager.IsMuted);
            _fullscreenToggle?.SetValueWithoutNotify(settingsManager.IsFullscreen);
        }

        private void InitializeResolutions()
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
                $"[NavBarSettingsController] {_availableResolutions.Count} resolutions. Selected: {target}",
                this
            );
        }

        private void ShowSection(Section section)
        {
            _activeSection = section;

            _displayContent.style.display =
                section == Section.Display ? DisplayStyle.Flex : DisplayStyle.None;
            _soundContent.style.display =
                section == Section.Sound ? DisplayStyle.Flex : DisplayStyle.None;

            SetNavSelected(_displayNavButton, section == Section.Display);
            SetNavSelected(_soundNavButton, section == Section.Sound);
        }

        private static void SetNavSelected(Button btn, bool selected)
        {
            if (selected)
                btn.AddToClassList("nav-button--selected");
            else
                btn.RemoveFromClassList("nav-button--selected");
        }

        private void RegisterCallbacks(bool register)
        {
            if (register)
            {
                _displayNavButton.clicked += OnDisplayNav;
                _soundNavButton.clicked += OnSoundNav;
                _exitButton.clicked += OnExitClicked;
                _closeButton.clicked += OnCloseClicked;

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
                if (_exitButton != null)
                    _exitButton.clicked -= OnExitClicked;
                if (_closeButton != null)
                    _closeButton.clicked -= OnCloseClicked;

                _fullscreenToggle?.UnregisterValueChangedCallback(OnFullscreenChanged);
                if (_resolutionSelect != null)
                    _resolutionSelect.OnValueChanged -= OnResolutionChanged;

                _volumeSlider?.UnregisterValueChangedCallback(OnVolumeChanged);
                _musicVolumeSlider?.UnregisterValueChangedCallback(OnMusicVolumeChanged);
                _sfxVolumeSlider?.UnregisterValueChangedCallback(OnSFXVolumeChanged);
                _muteToggle?.UnregisterValueChangedCallback(OnMuteChanged);
            }
        }

        private void OnDisplayNav() => ShowSection(Section.Display);

        private void OnSoundNav() => ShowSection(Section.Sound);

        private void OnCloseClicked()
        {
            logger.Log("[NavBarSettingsController] Close clicked.", this);
            Hide();
        }

        private void OnExitClicked()
        {
            Application.Quit();
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
        }

        private void OnMuteChanged(ChangeEvent<bool> evt)
        {
            settingsManager.IsMuted = evt.newValue;
            settingsManager.SaveSettings();
            float musicVol = evt.newValue ? 0f : settingsManager.MusicVolume;
            MusicPlayer.Instance?.SetGlobalMusicVolume(musicVol);
        }

        private void OnFullscreenChanged(ChangeEvent<bool> evt)
        {
            settingsManager.IsFullscreen = evt.newValue;
            settingsManager.SaveSettings();
            logger.Log($"[NavBarSettingsController] Fullscreen: {evt.newValue}", this);
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

            logger.Log(
                $"[NavBarSettingsController] Resolution: {target.width}x{target.height} @ {target.refreshRateRatio.value}Hz",
                this
            );
        }
    }
}
