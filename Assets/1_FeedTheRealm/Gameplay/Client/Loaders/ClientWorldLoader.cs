using System.Collections.Generic;
using API;
using FTR.Core.Client;
using FTR.Core.Client.EntryPoints;
using FTR.Core.Common.Config;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Client.EntryPoints;
using FTR.Gameplay.Common.LoaderEntities;
using VContainer;

namespace FTR.Gameplay.Client.Loaders
{
    public class ClientWorldLoader : ZoneLoaderManager
    {
        [Inject]
        private readonly Session.Session session;

        [Inject]
        private readonly WorldSelector worldSelector;

        public ClientWorldLoader(
            ClientPrefabProvider prefabProvider,
            ColliderRegistry colliderRegistry,
            ModelService modelService,
            MaterialService materialService,
            GltLoaderService gltfLoaderService,
            Session.Session session,
            Config config,
            IObjectResolver resolver,
            Logging.Logger logger
        )
        {
            var clientStructureLoader = new ClientStructureLoader(
                prefabProvider,
                colliderRegistry,
                modelService,
                gltfLoaderService,
                session,
                config
            );
            var clientNpcDialogLoader = new ClientNpcDialogLoader();
            var clientItemLoader = new ClientItemLoader();
            var clientQuestLoader = new ClientQuestLoader();
            var clientPortalLoader = new ClientPortalLoader(prefabProvider, resolver);
            var clientWorldAreaLoader = new ClientZoneAreaLoader(materialService, logger);

            loaders = new List<ILoader>
            {
                clientStructureLoader,
                clientNpcDialogLoader,
                clientItemLoader,
                clientQuestLoader,
                clientPortalLoader,
                clientWorldAreaLoader,
            };

            foreach (var loader in loaders)
            {
                resolver.Inject(loader);
            }
        }

        public override string GetWorldId()
        {
            if (config.DEBUG_IsDebugWorld)
                return !string.IsNullOrEmpty(config.DEBUG_WorldId)
                    ? config.DEBUG_WorldId
                    : worldSelector.GetSelectedWorldId();
            return worldSelector.GetSelectedWorldId();
        }

        public override string GetAccessToken()
        {
            return session.AccessToken;
        }

        public override int GetZoneId()
        {
            if (config.DEBUG_IsDebugWorld)
                return (config.DEBUG_ZoneId > 0)
                    ? config.DEBUG_ZoneId
                    : worldSelector.GetSelectedZoneId();
            return worldSelector.GetSelectedZoneId();
        }
    }
}
