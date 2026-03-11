using API;
using FeedTheRealm.Core.Client.EventChannels;
using FTR.Core.Client;
using FTR.Core.Common.Config;
using FTR.Gameplay.Client.Linkers;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.WorldLoader;
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
        private WorldSelector worldSelector;

        [SerializeField]
        private WorldService worldService;

        [SerializeField]
        private LoaderProvider loaderProvider;

        [SerializeField]
        private Session.Session session;

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
            builder.RegisterInstance(loaderProvider);
            builder.RegisterInstance(session);
            builder.Register<ClientPlayerLinker>(Lifetime.Singleton).As<PlayerLinker>();
            builder.Register<ClientAggresiveNpcLinker>(Lifetime.Singleton).As<AggresiveNpcLinker>();
            builder.Register<ClientPassiveNpcLinker>(Lifetime.Singleton).As<PassiveNpcLinker>();
            builder.Register<ClientLootItemLinker>(Lifetime.Singleton).As<LootItemLinker>();

            builder.RegisterEntryPoint<ClientWorldEntryPoint>();
        }
    }
}
