using System.Collections.Generic;
using FTR.Core.Common.Loaders;
using FTR.Core.Common.Utils;
using FTR.Core.Server;
using FTR.Gameplay.Common.LoaderEntities;
using FTR.Gameplay.Server.Loaders;
using FTR.Gameplay.Server.Registry;
using VContainer;

namespace FTR.Gameplay.Server.Scopes
{
    public class ServerWorldLoader : ZoneLoaderManager
    {
        public ServerWorldLoader(
            PortalRegistry portalRegistry,
            ServerPrefabProvider prefabProvider,
            IObjectResolver resolver
        )
        {
            var serverItemLoader = new ServerItemLoader();
            var structureLoader = new ServerStructureLoader(prefabProvider, resolver);
            var friendlyNpcSpawnerLoader = new FriendlyNpcSpawnerLoader(prefabProvider, resolver);
            var aggressiveNpcSpawnerLoader = new AggressiveNpcSpawnerLoader(
                prefabProvider,
                resolver
            );
            var playerSpawnerLoader = new PlayerSpawnerLoader();
            var portalLoader = new ServerPortalLoader(portalRegistry, prefabProvider, resolver);

            loaders = new List<ILoader>
            {
                serverItemLoader,
                structureLoader,
                friendlyNpcSpawnerLoader,
                aggressiveNpcSpawnerLoader,
                playerSpawnerLoader,
                portalLoader,
            };

            foreach (var loader in loaders)
            {
                resolver.Inject(loader);
            }
        }

        public override string GetWorldId()
        {
            if (config.IsDebugWorld)
                return ParamsSerializer.GetArgs("world-id", config.WorldID);
            return ParamsSerializer.GetArgs("world-id", null);
        }

        public override string GetAccessToken()
        {
            if (string.IsNullOrEmpty(config.ServerAccessToken))
                throw new System.InvalidOperationException(
                    "Access token is required to load the world. Please provide it in the config or as a command line argument."
                );
            return config.ServerAccessToken;
        }

        public override int GetZoneId()
        {
            if (config.IsDebugWorld)
                return int.Parse(ParamsSerializer.GetArgs("zone-id", config.ZoneID.ToString()));
            return int.Parse(ParamsSerializer.GetArgs("zone-id", null));
        }
    }
}
