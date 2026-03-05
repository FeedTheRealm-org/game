using System.Collections.Generic;
using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Config;
using FTR.Core.Common.Loaders;
using FTR.Core.Server.Utils;
using FTR.Gameplay.Common.WorldLoader;
using FTR.Gameplay.LoaderEntities;
using FTRShared.Runtime.Models;
using Logging;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Scopes
{
    public class ServerWorldLoader : WorldLoader
    {
        private readonly Config config;

        public ServerWorldLoader(
            WorldService worldService,
            Logger logger,
            Config config,
            LoaderProvider loaderProvider
        )
            : base(worldService, logger, loaderProvider)
        {
            this.config = config;
        }

        public override string GetWorldId()
        {
            if (config.IsDebugWorld)
                return config.WorldID;
            return ParamsSerializer.GetArgs("worldId", null);
        }

        public override string GetAccessToken()
        {
            if (config.IsDebugWorld)
                return config.AccessToken;
            return ParamsSerializer.GetArgs("accessToken", null);
        }
    }
}
