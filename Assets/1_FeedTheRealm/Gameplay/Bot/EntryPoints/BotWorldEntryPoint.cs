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
        private readonly BotConfig botConfig;

        public BotWorldEntryPoint(Logging.Logger logger, Config config, BotConfig botConfig)
        {
            this.logger = logger;
            this.config = config;
            this.botConfig = botConfig;
        }

        public void Start()
        {
            logger.Log(
                $"[BotWorldEntryPoint] Starting bot behavior. botId={botConfig.BotId}, worldId={botConfig.WorldId}"
            );
            WorldLoadBootstrap.MarkClientReady();
        }
    }
}
