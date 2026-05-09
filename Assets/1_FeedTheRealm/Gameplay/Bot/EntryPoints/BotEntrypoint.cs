using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Bot.Config;
using FTR.Core.Common.Config;
using Session;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace FTR.Gameplay.Bot.EntryPoints
{
    public class BotEntryPoint : IStartable
    {
        private readonly SceneReference mainScene;
        private readonly Logging.Logger logger;
        private readonly Config config;
        private readonly BotConfig botConfig;
        private readonly WorldService worldService;
        private readonly AuthService authService;
        private readonly Session.Session session;

        public BotEntryPoint(
            SceneReference mainScene,
            Logging.Logger logger,
            Config config,
            BotConfig botConfig,
            WorldService worldService,
            AuthService authService,
            Session.Session session
        )
        {
            this.mainScene = mainScene;
            this.logger = logger;
            this.config = config;
            this.botConfig = botConfig;
            this.worldService = worldService;
            this.authService = authService;
            this.session = session;
        }

        public async void Start()
        {
            botConfig.LoadParams();
            ConfigureUnityForBot();

            try
            {
                session.AccessToken = botConfig.AdminToken;
                var (ip, port, err, _) = await worldService.GetZoneAddress(
                    botConfig.WorldId,
                    botConfig.ZoneId
                );
                if (!string.IsNullOrEmpty(err))
                    throw new System.Exception($"Failed to get zone address: {err}");
                var authErr = await authService.Login(
                    botConfig.BotEmail,
                    botConfig.BotPassword,
                    botConfig.AdminToken
                );
                if (!string.IsNullOrEmpty(authErr))
                    throw new System.Exception(
                        $"Failed to login bot {botConfig.BotEmail}: {authErr}"
                    );
                config.CurrentServerAddress = ip;
                config.CurrentServerPort = (ushort)port;
                await LoadMainScene();
            }
            catch (System.Exception ex)
            {
                logger.Log($"Failed to start bot: {ex.Message}", Logging.LogType.Error);
                Application.Quit();
            }
        }

        void ConfigureUnityForBot()
        {
            // Cap Update & LateUpdate TPS
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 15;
        }

        async UniTask LoadMainScene()
        {
            await SceneManager.LoadSceneAsync(mainScene.SceneName, LoadSceneMode.Single);
        }
    }
}
