using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Bot.Config;
using FTR.Core.Common.Config;
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

        public BotEntryPoint(
            SceneReference mainScene,
            Logging.Logger logger,
            Config config,
            BotConfig botConfig,
            WorldService worldService
        )
        {
            this.mainScene = mainScene;
            this.logger = logger;
            this.config = config;
            this.botConfig = botConfig;
            this.worldService = worldService;
        }

        public async void Start()
        {
            botConfig.LoadParams();
            ConfigureUnityForBot();

            try
            {
                var (ip, port, err, _) = await worldService.GetZoneAddress(
                    botConfig.WorldId,
                    botConfig.ZoneId,
                    botConfig.ServerFixedToken
                );
                if (!string.IsNullOrEmpty(err))
                    throw new System.Exception($"Failed to get zone address: {err}");
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
