using Cysharp.Threading.Tasks;
using FeedTheRealm.Gameplay.Client.SceneSetup;
using FTR.Core.Client.EventChannels.UI;
using FTR.Core.Client.Interfaces;
using FTR.Core.Client.Managers;
using FTR.Core.Client.Settings;
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
        private readonly GameObject navBarPrefab;
        private readonly GameObject profileMenuPrefab;
        private readonly GameObject gemStorePrefab;
        private readonly GameObject musicPlayerPrefab;
        private readonly ClientMusicRegistry musicRegistry;
        private readonly GameObject loadingScreenPrefab;
        private readonly MainMenuFlowService flowService;
        private readonly SettingsManager settingsManager;
        private readonly GameObject navBarSettingsPrefab;
        private readonly GameObject authBackgroundPrefab;
        private readonly GameObject confirmPopupPrefab;
        private readonly ConfirmPopupHandle confirmPopupHandle;
        private MenuManager menuManager;
        private IObjectResolver objectResolver;

        public ClientEntryPoint(
            SceneReference mainScene,
            Session.Session session,
            API.AuthService authService,
            GameObject worldFeedMenuPrefab,
            GameObject navBarPrefab,
            GameObject profileMenuPrefab,
            GameObject gemStorePrefab,
            GameObject musicPlayerPrefab,
            ClientMusicRegistry musicRegistry,
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
            IObjectResolver resolver
        )
        {
            this.mainScene = mainScene;
            this.session = session;
            this.authService = authService;
            this.worldFeedMenuPrefab = worldFeedMenuPrefab;
            this.navBarPrefab = navBarPrefab;
            this.profileMenuPrefab = profileMenuPrefab;
            this.gemStorePrefab = gemStorePrefab;
            this.musicPlayerPrefab = musicPlayerPrefab;
            this.musicRegistry = musicRegistry;
            this.loadingScreenPrefab = loadingScreenPrefab;
            this.settingsManager = settingsManager;
            this.navBarSettingsPrefab = navBarSettingsPrefab;
            this.authBackgroundPrefab = authBackgroundPrefab;
            this.menuManager = menuManager;
            this.confirmPopupHandle = confirmPopupHandle;
            this.confirmPopupPrefab = confirmPopupPrefab;
            this.objectResolver = resolver;

            flowService = new MainMenuFlowService(
                worldFeedMenuPrefab,
                navBarPrefab,
                profileMenuPrefab,
                gemStorePrefab,
                musicPlayerPrefab,
                musicRegistry,
                navBarSettingsPrefab,
                authFlowManager,
                playerService,
                onProfileCreatedEvent,
                onLogoutRequestedEvent,
                resolver
            );
        }

        public async void Start()
        {
            ConfigureUnityForClient();

            menuManager.SetIsMainMenu(true);

            var confirmPopupObj = objectResolver.Instantiate(confirmPopupPrefab);
            confirmPopupHandle.Controller = confirmPopupObj.GetComponent<IConfirmPopup>();

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

            if (loadingScreenInstance != null)
            {
                Object.Destroy(loadingScreenInstance);
            }
        }

        void ConfigureUnityForClient()
        {
            // TODO: Load client config

            // Cap Update & LateUpdate TPS
            settingsManager.LoadSettings();
            settingsManager.ApplyDisplay();
            settingsManager.ApplyAudioListener();
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
    }
}
