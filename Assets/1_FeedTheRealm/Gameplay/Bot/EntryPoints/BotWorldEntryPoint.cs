using FTR.Core.Bot.Config;
using FTR.Core.Common.Config;
using FTR.Core.Common.Scopes;
using VContainer.Unity;

namespace FTR.Gameplay.Bot.EntryPoints
{
    public class BotWorldEntryPoint : IStartable
    {
        private readonly Logging.Logger logger;
        private readonly Config config;
        private readonly BotRuntimeConfig runtimeConfig;

        public BotWorldEntryPoint(
            Logging.Logger logger,
            Config config,
            BotRuntimeConfig runtimeConfig
        )
        {
            this.logger = logger;
            this.config = config;
            this.runtimeConfig = runtimeConfig;
        }

        public void Start()
        {
            if (!string.IsNullOrWhiteSpace(runtimeConfig.ServerAddress))
                config.CurrentServerAddress = runtimeConfig.ServerAddress;

            if (runtimeConfig.ServerPort > 0)
                config.CurrentServerPort = runtimeConfig.ServerPort;

            logger.Log(
                $"[BotWorldEntryPoint] Starting bot behavior. botId={runtimeConfig.BotId}, worldId={runtimeConfig.WorldId}, zoneId={runtimeConfig.ZoneId}, server={config.CurrentServerAddress}:{config.CurrentServerPort}"
            );

            if (string.IsNullOrWhiteSpace(runtimeConfig.JoinToken))
            {
                logger.Log(
                    "[BotWorldEntryPoint] --join-token is missing. Bot can move/interact but server-side user identity resolution will be skipped.",
                    Logging.LogType.Warning
                );
            }

            WorldLoadBootstrap.MarkClientReady();
        }
    }
}
