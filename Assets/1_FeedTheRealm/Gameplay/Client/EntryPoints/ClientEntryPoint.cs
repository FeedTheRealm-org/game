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
        private readonly GameObject loginPrefab;
        private readonly GameObject signUpPrefab;
        private readonly GameObject verifyCodePrefab;
        private readonly GameObject worldFeedMenuPrefab;
        private readonly GameObject navBarPrefab;
        private readonly GameObject profileMenuPrefab;
        private readonly GameObject gemStorePrefab;
        private readonly GameObject musicPlayerPrefab;
        private readonly ClientMusicRegistry musicRegistry;
        private readonly MainMenuFlowService flowService;

        public ClientEntryPoint(
            SceneReference mainScene,
            GameObject loginPrefab,
            GameObject signUpPrefab,
            GameObject verifyCodePrefab,
            GameObject worldFeedMenuPrefab,
            GameObject navBarPrefab,
            GameObject profileMenuPrefab,
            GameObject gemStorePrefab,
            GameObject musicPlayerPrefab,
            ClientMusicRegistry musicRegistry
        )
        {
            this.mainScene = mainScene;
            this.loginPrefab = loginPrefab;
            this.signUpPrefab = signUpPrefab;
            this.verifyCodePrefab = verifyCodePrefab;
            this.worldFeedMenuPrefab = worldFeedMenuPrefab;
            this.navBarPrefab = navBarPrefab;
            this.profileMenuPrefab = profileMenuPrefab;
            this.gemStorePrefab = gemStorePrefab;
            this.musicPlayerPrefab = musicPlayerPrefab;
            this.musicRegistry = musicRegistry;

            flowService = new MainMenuFlowService(
                loginPrefab,
                signUpPrefab,
                verifyCodePrefab,
                worldFeedMenuPrefab,
                navBarPrefab,
                profileMenuPrefab,
                gemStorePrefab,
                musicPlayerPrefab,
                musicRegistry
            );
        }

        public async void Start()
        {
            ConfigureUnityForClient();

            flowService.InitializeMusicPlayer(MusicType.Menu);

            await flowService.ShowAuthFlow();
            await flowService.ShowMainMenuFlow();

            await flowService.DestroyMusicPlayerAsync(fadeOut: true);

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
