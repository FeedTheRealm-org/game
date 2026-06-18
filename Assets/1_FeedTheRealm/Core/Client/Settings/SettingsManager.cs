using FTR.Core.Common.EventChannels;
using FTRShared.Runtime.Models.Settings;
using UnityEngine;

namespace FTR.Core.Client.Settings
{
    [CreateAssetMenu(fileName = "SettingsManager", menuName = "Scriptable Objects/SettingsManager")]
    public class SettingsManager : ScriptableObject
    {
        public float GlobalVolume { get; set; } = 1f;
        public float MusicVolume { get; set; } = 1f;
        public float SFXVolume { get; set; } = 1f;
        public bool ShowPerformanceStats { get; set; } = false;
        public bool IsMuted { get; set; } = false;

        public int ResolutionWidth { get; set; }
        public int ResolutionHeight { get; set; }
        public RefreshRate RefreshRate { get; set; }
        public bool IsFullscreen { get; set; } = true;

        public bool IsCachingEnabled { get; set; } = true;

        private SettingsRepository _repository;

        [SerializeField]
        private PerformanceStatsToggleEvent performanceStatsToggleEvent;

        private void OnEnable() => _repository = new SettingsRepository();

        public void LoadSettings()
        {
            var data = _repository.Load();

            if (data == null)
            {
                GlobalVolume = 1f;
                MusicVolume = 1f;
                SFXVolume = 1f;
                IsMuted = false;
                ResolutionWidth = Screen.currentResolution.width;
                ResolutionHeight = Screen.currentResolution.height;
                RefreshRate = Screen.currentResolution.refreshRateRatio;
                IsFullscreen = Screen.fullScreen;
                IsCachingEnabled = true;
                ShowPerformanceStats = false;
                performanceStatsToggleEvent.Raise(ShowPerformanceStats);
                return;
            }

            GlobalVolume = data.globalVolume;
            MusicVolume = data.musicVolume;
            SFXVolume = data.sfxVolume;
            IsMuted = data.isMuted;
            ResolutionWidth = data.resolutionWidth;
            ResolutionHeight = data.resolutionHeight;
            RefreshRate = data.refreshRate.ToRefreshRate();
            IsFullscreen = data.isFullscreen;
            IsCachingEnabled = data.enableCaching;
            ShowPerformanceStats = data.showPerformanceStats;

            performanceStatsToggleEvent.Raise(ShowPerformanceStats);
        }

        public void SaveSettings()
        {
            _repository.Save(
                new SettingsData
                {
                    globalVolume = GlobalVolume,
                    musicVolume = MusicVolume,
                    sfxVolume = SFXVolume,
                    isMuted = IsMuted,
                    resolutionWidth = ResolutionWidth,
                    resolutionHeight = ResolutionHeight,
                    refreshRate = RefreshRateData.FromRefreshRate(RefreshRate),
                    isFullscreen = IsFullscreen,
                    enableCaching = IsCachingEnabled,
                    showPerformanceStats = ShowPerformanceStats,
                }
            );

            ApplyDisplay();
            ApplyAudioListener();
        }

        public void ApplyDisplay()
        {
            if (ResolutionWidth > 0 && ResolutionHeight > 0)
            {
                Screen.SetResolution(
                    ResolutionWidth,
                    ResolutionHeight,
                    IsFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed,
                    RefreshRate
                );
            }
            else
            {
                Screen.fullScreen = IsFullscreen;
            }
            performanceStatsToggleEvent.Raise(ShowPerformanceStats);
        }

        public void ApplyAudioListener()
        {
            AudioListener.volume = IsMuted ? 0f : GlobalVolume;
        }
    }
}
