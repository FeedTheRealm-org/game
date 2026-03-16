using API;
using FeedTheRealm.Core.Client.EventChannels;
using FTR.Core.Client;
using FTR.Core.Common.Config;
using FTR.Gameplay.Client.Linkers;
using FTR.Gameplay.Client.Loaders;
using FTR.Gameplay.Common.Linkers;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.EntryPoints.Scopes
{
    public class ClientWorldInitiator : LifetimeScope
    {
        [SerializeField]
        private Config config;

        [SerializeField]
        private PlayerInputReader playerInputReader;

        [SerializeField]
        private ClientPrefabProvider prefabProvider;

        [SerializeField]
        private ClientEventRegistry clientEventRegistry;

        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private Session.Session session;

        [SerializeField]
        private WorldSelector worldSelector;

        [SerializeField]
        private WorldService worldService;

        [SerializeField]
        private ModelService modelService;

        [SerializeField]
        private GltLoaderService gltfLoaderService;

        protected override void Configure(IContainerBuilder builder)
        {
            if (config.RuntimeRole != RuntimeRole.Client)
                return;

            clientEventRegistry.RegisterAll(builder);
            builder.RegisterInstance(playerInputReader);
            builder.RegisterInstance(prefabProvider);
            builder.RegisterInstance(logger);
            builder.RegisterInstance(worldSelector);
            builder.RegisterInstance(worldService);
            builder.RegisterInstance(config);
            builder.RegisterInstance(session);
            builder.RegisterInstance(modelService);
            builder.RegisterInstance(gltfLoaderService);
            builder.Register<ClientWorldLoader>(Lifetime.Singleton);
            builder.Register<ClientPlayerLinker>(Lifetime.Singleton).As<PlayerLinker>();
            builder.Register<ClientAggresiveNpcLinker>(Lifetime.Singleton).As<AggresiveNpcLinker>();
            builder.Register<ClientPassiveNpcLinker>(Lifetime.Singleton).As<PassiveNpcLinker>();
            builder.Register<ClientLootItemLinker>(Lifetime.Singleton).As<LootItemLinker>();

            builder.RegisterEntryPoint<ClientWorldEntryPoint>();
        }
    }
}
