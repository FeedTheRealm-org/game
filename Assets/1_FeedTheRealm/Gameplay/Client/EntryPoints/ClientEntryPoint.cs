using Cysharp.Threading.Tasks;
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
        private readonly MainMenuFlowService flowService;

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
            GameObject gemStorePrefab
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
            flowService = new MainMenuFlowService(
                loginPrefab,
                signUpPrefab,
                verifyCodePrefab,
                worldFeedMenuPrefab,
                navBarPrefab,
                profileMenuPrefab,
                gemStorePrefab
            );
        }

        public async void Start()
        {
            ConfigureUnityForClient();

            await flowService.ShowAuthFlow(authService, session);
            await flowService.ShowMainMenuFlow();
            await LoadMainScene();
        }

        void ConfigureUnityForClient()
        {
            // TODO: Load client config

            // Cap Update & LateUpdate TPS
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        async UniTask LoadMainScene()
        {
            await SceneManager.LoadSceneAsync(mainScene.SceneName, LoadSceneMode.Single);
        }
    }
}
