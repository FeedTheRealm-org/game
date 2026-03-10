using API;
using FTR.Core.Common.Config;
using FTR.Core.Server.Utils;
using FTR.Gameplay.Common.LoaderEntities;
using FTR.Gameplay.Common.WorldLoader;

namespace FTR.Gameplay.Server.Scopes
{
    public class ServerWorldLoader : WorldLoader
    {
        private readonly Config config;

        public ServerWorldLoader(
            WorldService worldService,
            Logging.Logger logger,
            Config config,
            LoaderProvider loaderProvider
        )
            : base(config, worldService, logger, loaderProvider)
        {
            this.config = config;
        }

        public override string GetWorldId()
        {
            if (config.IsDebugWorld)
                return ParamsSerializer.GetArgs("worldId", config.WorldID);
            return ParamsSerializer.GetArgs("worldId", null);
        }

        public override string GetAccessToken()
        {
            if (config.IsDebugWorld)
                return ParamsSerializer.GetArgs("accessToken", config.AccessToken);
            return ParamsSerializer.GetArgs("accessToken", null);
        }
    }
}
