using Cysharp.Threading.Tasks;
using FeedTheRealm.Gameplay.Client.SceneSetup;
using FTR.Core.Client.EventChannels.UI;
using FTR.Core.Client.Interfaces;
using FTR.Core.Client.Managers;
using FTR.Core.Client.Settings;
using FTR.Gameplay.Client.Registry;
using FTRShared.Runtime.Core.Cache;
using FTRShared.UI.AuthMenu;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.EntryPoints
{
    /// <summary>
    /// Entry point for the client application, responsible for initializing the application flow, including authentication and main menu navigation.
    /// </summary>
    public class ClientEntryPoint : IStartable
    {
        private readonly SceneReference mainScene;
        public readonly Session.Session session;
        public readonly API.AuthService authService;
        private readonly GameObject worldFeedMenuPrefab;
        private readonly GameObject worldInfoMenuPrefab;
        private readonly GameObject navBarPrefab;
        private readonly GameObject profileMenuPrefab;
        private readonly GameObject gemStorePrefab;
        private readonly GameObject musicPlayerPrefab;
        private readonly ClientMusicRegistry musicRegistry;
        private readonly ClientSoundFXRegistry soundFXRegistry;
        private readonly ISoundPlayer soundPlayer;
        private readonly GameObject loadingScreenPrefab;
        private readonly MainMenuFlowService flowService;
        private readonly SettingsManager settingsManager;
        private readonly GameObject navBarSettingsPrefab;
        private readonly GameObject authBackgroundPrefab;

        private readonly GameObject confirmPopupPrefab;
        private readonly ConfirmPopupHandle confirmPopupHandle;
        private readonly CacheManager cacheManager;
        private MenuManager menuManager;
        private readonly WorldInfoMenuHandle worldInfoMenuHandle;
        private readonly IObjectResolver resolver;

        public ClientEntryPoint(
            SceneReference mainScene,
            Session.Session session,
            API.AuthService authService,
            GameObject worldFeedMenuPrefab,
            GameObject worldInfoMenuPrefab,
            GameObject navBarPrefab,
            GameObject profileMenuPrefab,
            GameObject gemStorePrefab,
            GameObject musicPlayerPrefab,
            ClientMusicRegistry musicRegistry,
            ClientSoundFXRegistry soundFXRegistry,
            ISoundPlayer soundPlayer,
            GameObject loadingScreenPrefab,
            SettingsManager settingsManager,
            GameObject navBarSettingsPrefab,
            AuthFlowManager authFlowManager,
            GameObject authBackgroundPrefab,
            API.PlayerService playerService,
            OnProfileCreatedEvent onProfileCreatedEvent,
            OnLogoutRequestedEvent onLogoutRequestedEvent,
            MenuManager menuManager,
            ConfirmPopupHandle confirmPopupHandle,
            GameObject confirmPopupPrefab,
            CacheManager cacheManager,
            WorldInfoMenuHandle worldInfoMenuHandle,
            IObjectResolver resolver
        )
        {
            this.mainScene = mainScene;
            this.session = session;
            this.authService = authService;
            this.worldFeedMenuPrefab = worldFeedMenuPrefab;
            this.worldInfoMenuPrefab = worldInfoMenuPrefab;
            this.navBarPrefab = navBarPrefab;
            this.profileMenuPrefab = profileMenuPrefab;
            this.gemStorePrefab = gemStorePrefab;
            this.musicPlayerPrefab = musicPlayerPrefab;
            this.musicRegistry = musicRegistry;
            this.soundFXRegistry = soundFXRegistry;
            this.soundPlayer = soundPlayer;
            this.loadingScreenPrefab = loadingScreenPrefab;
            this.settingsManager = settingsManager;
            this.navBarSettingsPrefab = navBarSettingsPrefab;
            this.authBackgroundPrefab = authBackgroundPrefab;
            this.menuManager = menuManager;
            this.confirmPopupHandle = confirmPopupHandle;
            this.confirmPopupPrefab = confirmPopupPrefab;
            this.cacheManager = cacheManager;
            this.worldInfoMenuHandle = worldInfoMenuHandle;
            this.resolver = resolver;

            flowService = new MainMenuFlowService(
                worldFeedMenuPrefab,
                worldInfoMenuPrefab,
                navBarPrefab,
                profileMenuPrefab,
                gemStorePrefab,
                musicPlayerPrefab,
                musicRegistry,
                soundFXRegistry,
                soundPlayer,
                navBarSettingsPrefab,
                authFlowManager,
                playerService,
                onProfileCreatedEvent,
                onLogoutRequestedEvent,
                worldInfoMenuHandle,
                resolver
            );
        }

        public async void Start()
        {
            ConfigureUnityForClient();

            menuManager.SetIsMainMenu(true);

            var confirmPopupObj = resolver.Instantiate(confirmPopupPrefab);
            SetupConfirmPopup(confirmPopupObj);

            flowService.InitializeMusicPlayer(MusicType.Menu);

            while (true)
            {
                await flowService.ShowAuthFlow(authService, session, authBackgroundPrefab);
                bool loggedOut = await flowService.ShowMainMenuFlow();

                if (!loggedOut)
                    break;
            }

            GameObject loadingScreenInstance = SetupLoadingScreen();

            await flowService.DestroyMusicPlayerAsync(fadeOut: true);

            await LoadMainScene();
        }

        void ConfigureUnityForClient()
        {
            // TODO: Load client config

            // Cap Update & LateUpdate TPS
            settingsManager.LoadSettings();
            settingsManager.ApplyDisplay();
            settingsManager.ApplyAudioListener();
            cacheManager.SetCachingEnabled(settingsManager.IsCachingEnabled);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
        }

        async UniTask LoadMainScene()
        {
            await SceneManager.LoadSceneAsync(mainScene.SceneName, LoadSceneMode.Single);
        }

        private GameObject SetupLoadingScreen()
        {
            GameObject loadingScreenInstance = null;
            if (loadingScreenPrefab != null)
            {
                loadingScreenInstance = Object.Instantiate(loadingScreenPrefab);
                Object.DontDestroyOnLoad(loadingScreenInstance);
                var loadingScreenDoc =
                    loadingScreenInstance.GetComponent<UnityEngine.UIElements.UIDocument>();
                if (loadingScreenDoc != null && loadingScreenDoc.rootVisualElement != null)
                {
                    loadingScreenDoc.rootVisualElement.style.display = UnityEngine
                        .UIElements
                        .DisplayStyle
                        .Flex;
                }
            }

            return loadingScreenInstance;
        }

        private void SetupConfirmPopup(GameObject confirmPopupObj)
        {
            var confirmPopupController = confirmPopupObj.GetComponent<IConfirmPopup>();
            if (confirmPopupController == null)
            {
                string errorMessage =
                    $"Confirm popup prefab '{confirmPopupPrefab.name}' does not implement {nameof(IConfirmPopup)}.";
                Debug.LogError(errorMessage, confirmPopupObj);
                throw new MissingComponentException(errorMessage);
            }
            confirmPopupHandle.Controller = confirmPopupController;
        }
    }
}
