using System.Collections.Generic;
using FTR.Core.Common.Loaders;
using FTR.Core.Common.Utils;
using FTR.Core.Server;
using FTR.Core.Server.Config;
using FTR.Gameplay.Common.LoaderEntities;
using FTR.Gameplay.Server.Loaders;
using FTR.Gameplay.Server.Registry;
using VContainer;

namespace FTR.Gameplay.Server.Scopes
{
    public class ServerWorldLoader : ZoneLoaderManager
    {
        private readonly ServerConfig serverConfig;
        private readonly ServerSecretsConfig secretsConfig;
        private readonly Session.Session session;

        public ServerWorldLoader(
            PortalRegistry portalRegistry,
            ServerPrefabProvider prefabProvider,
            ColliderRegistry colliderRegistry,
            IObjectResolver resolver,
            Session.Session session
        )
        {
            this.session = session;
            if (!resolver.TryResolve(out serverConfig))
                throw new System.InvalidOperationException(
                    "ServerConfig is required to load the world. Please provide it in the container."
                );
            if (!resolver.TryResolve(out secretsConfig))
                throw new System.InvalidOperationException(
                    "ServerSecretsConfig is required to load the world. Please provide it in the container."
                );

            var serverItemLoader = new ServerItemLoader();
            var structureLoader = new ServerStructureLoader(
                prefabProvider,
                colliderRegistry,
                resolver
            );
            var friendlyNpcSpawnerLoader = new FriendlyNpcSpawnerLoader(prefabProvider, resolver);
            var aggressiveNpcSpawnerLoader = new AggressiveNpcSpawnerLoader(
                prefabProvider,
                resolver
            );
            var playerSpawnerLoader = new PlayerSpawnerLoader();
            var portalLoader = new ServerPortalLoader(portalRegistry, prefabProvider, resolver);
            var chestLoader = new ServerChestLoader(prefabProvider, resolver);

            loaders = new List<ILoader>
            {
                serverItemLoader,
                structureLoader,
                friendlyNpcSpawnerLoader,
                aggressiveNpcSpawnerLoader,
                playerSpawnerLoader,
                portalLoader,
                chestLoader,
            };

            foreach (var loader in loaders)
            {
                resolver.Inject(loader);
            }
        }

        public override string GetWorldId()
        {
            if (config.DEBUG_IsDebugWorld)
                return !string.IsNullOrEmpty(serverConfig.WorldId)
                    ? serverConfig.WorldId
                    : config.DEBUG_WorldId;
            return serverConfig.WorldId;
        }

        public override string GetAccessToken()
        {
            if (string.IsNullOrEmpty(secretsConfig.ServerFixedToken))
                throw new System.InvalidOperationException(
                    "Access token is required to load the world. Please provide it in the config or as a command line argument."
                );
            session.AccessToken = secretsConfig.ServerFixedToken;
            return secretsConfig.ServerFixedToken;
        }

        public override int GetZoneId()
        {
            if (config.DEBUG_IsDebugWorld)
                return serverConfig.ZoneId > 0 ? serverConfig.ZoneId : config.DEBUG_ZoneId;
            return serverConfig.ZoneId;
        }
    }
}
