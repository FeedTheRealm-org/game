using Cysharp.Threading.Tasks;
using FTR.Core.Bot.Config;
using FTR.Core.Common.Config;
using FTR.Core.Common.Scopes;
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

        public BotEntryPoint(
            SceneReference mainScene,
            Logging.Logger logger,
            Config config,
            BotConfig botConfig
        )
        {
            this.mainScene = mainScene;
            this.logger = logger;
            this.config = config;
            this.botConfig = botConfig;
        }

        public async void Start()
        {
            botConfig.LoadParams();
            ConfigureUnityForBot();

            config.CurrentServerAddress = "localhost";
            config.CurrentServerPort = 7777;

            await LoadMainScene();
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
