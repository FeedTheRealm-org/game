using System.Collections.Generic;
using FTR.Core.Client;
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

        public ClientWorldLoader(ClientPrefabProvider prefabProvider, IObjectResolver resolver)
        {
            var clientStructureLoader = new ClientStructureLoader(prefabProvider);
            var clientNpcDialogLoader = new ClientNpcDialogLoader();
            var clientItemLoader = new ClientItemLoader();
            var clientQuestLoader = new ClientQuestLoader();
            var ClientPortalLoader = new ClientPortalLoader(prefabProvider, resolver);

            loaders = new List<ILoader>
            {
                clientStructureLoader,
                clientNpcDialogLoader,
                clientItemLoader,
                clientQuestLoader,
                ClientPortalLoader,
            };

            foreach (var loader in loaders)
            {
                resolver.Inject(loader);
            }
        }

        public override string GetWorldId()
        {
            if (config.IsDebugWorld)
                return !string.IsNullOrEmpty(config.WorldID)
                    ? config.WorldID
                    : worldSelector.GetSelectedWorldId();
            return worldSelector.GetSelectedWorldId();
        }

        public override string GetAccessToken()
        {
            if (config.IsDebugWorld)
                return !string.IsNullOrEmpty(config.ServerAccessToken)
                    ? config.ServerAccessToken
                    : session.APIToken;
            return session.APIToken;
        }
    }
}
