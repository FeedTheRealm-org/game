using System.Collections.Generic;
using API;
using Cysharp.Threading.Tasks;
using FTR.Core.Common.Config;
using FTR.Core.Common.Loaders;
using FTR.Core.Server.Utils;
using FTR.Gameplay.Common.LoaderEntities;
using FTR.Gameplay.Common.WorldLoader;
using FTRShared.Runtime.Models;
using Logging;
using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.Scopes
{
    public class ServerWorldLoader : WorldLoader
    {
        private readonly IObjectResolver container;
        private readonly GameObject debugObjectPrefab;
        private readonly Config config;

        public ServerWorldLoader(
            WorldService worldService,
            Logging.Logger logger,
            Config config,
            LoaderProvider loaderProvider,
            GameObject debugObjectPrefab,
            IObjectResolver container
        )
            : base(worldService, logger, loaderProvider)
        {
            this.container = container;
            this.debugObjectPrefab = debugObjectPrefab;
            this.config = config;
        }

        protected override void Initialize()
        {
            base.Initialize();
            if (config.IsDebugWorld && debugObjectPrefab != null)
            {
                GameObject debugObjectInstance = container.Instantiate(
                    debugObjectPrefab,
                    new Vector3(20, 5f, 0), // random position for testing
                    Quaternion.identity
                );
                NetworkServer.Spawn(debugObjectInstance);
            }
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
