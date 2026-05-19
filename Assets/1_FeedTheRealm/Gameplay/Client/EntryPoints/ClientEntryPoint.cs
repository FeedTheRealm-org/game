using Cysharp.Threading.Tasks;
using FTR.Core.Client.Settings;
using FTRShared.UI.AuthMenu;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        private readonly GameObject loginPrefab;
        private readonly GameObject signUpPrefab;
        private readonly GameObject verifyCodePrefab;
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

        public ClientEntryPoint(
            SceneReference mainScene,
            Session.Session session,
            API.AuthService authService,
            GameObject loginPrefab,
            GameObject signUpPrefab,
            GameObject verifyCodePrefab,
            GameObject worldFeedMenuPrefab,
            GameObject navBarPrefab,
            GameObject profileMenuPrefab,
            GameObject gemStorePrefab,
            GameObject musicPlayerPrefab,
            ClientMusicRegistry musicRegistry,
            GameObject loadingScreenPrefab,
            SettingsManager settingsManager,
            GameObject navBarSettingsPrefab
        )
        {
            this.mainScene = mainScene;
            this.session = session;
            this.authService = authService;
            this.loginPrefab = loginPrefab;
            this.signUpPrefab = signUpPrefab;
            this.verifyCodePrefab = verifyCodePrefab;
            this.worldFeedMenuPrefab = worldFeedMenuPrefab;
            this.navBarPrefab = navBarPrefab;
            this.profileMenuPrefab = profileMenuPrefab;
            this.gemStorePrefab = gemStorePrefab;
            this.musicPlayerPrefab = musicPlayerPrefab;
            this.musicRegistry = musicRegistry;
            this.loadingScreenPrefab = loadingScreenPrefab;
            this.settingsManager = settingsManager;
            this.navBarSettingsPrefab = navBarSettingsPrefab;

            flowService = new MainMenuFlowService(
                loginPrefab,
                signUpPrefab,
                verifyCodePrefab,
                worldFeedMenuPrefab,
                navBarPrefab,
                profileMenuPrefab,
                gemStorePrefab,
                musicPlayerPrefab,
                musicRegistry,
                navBarSettingsPrefab
            );
        }

        public async void Start()
        {
            ConfigureUnityForClient();

            flowService.InitializeMusicPlayer(MusicType.Menu);

            await flowService.ShowAuthFlow(authService, session);
            await flowService.ShowMainMenuFlow();

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
