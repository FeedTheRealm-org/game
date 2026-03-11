using API;
using FTR.Core.Common.Config;
using FTR.Core.Server;
using FTR.Core.Server.EventChannels;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Common.WorldLoader;
using FTR.Gameplay.Server.Linkers;
using FTR.Gameplay.Server.Scopes;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Server.EntryPoints.Scopes
{
    public class ServerWorldInitiator : LifetimeScope
    {
        [SerializeField]
        private Config config;

        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private GameTickEvent gameTickEvent;

        [SerializeField]
        private ServerPrefabProvider prefabProvider;

        [SerializeField]
        private LoaderProvider loaderProvider;

        [SerializeField]
        private WorldService worldService;

        // This is only used for testing purposes,
        // to spawn objects in the world and test interactions
        // without needing to implement the full world loading logic.
        [Header("Testing / Debug")]
        public GameObject debugObjectPrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            if (config.RuntimeRole != RuntimeRole.Server)
                return;

            builder.RegisterInstance(logger);
            builder.RegisterInstance(gameTickEvent);
            builder.RegisterInstance(prefabProvider);
            builder.RegisterInstance(loaderProvider);
            builder.RegisterInstance(worldService);
            builder.Register<WorldMonitor>(Lifetime.Singleton);

            builder.Register<ServerPlayerLinker>(Lifetime.Singleton).As<PlayerLinker>();
            builder.Register<ServerAggresiveNpcLinker>(Lifetime.Singleton).As<AggresiveNpcLinker>();
            builder.Register<ServerPassiveNpcLinker>(Lifetime.Singleton).As<PassiveNpcLinker>();
            builder.Register<ServerLootItemLinker>(Lifetime.Singleton).As<LootItemLinker>();

            builder.Register<GameLoop>(Lifetime.Singleton);
            builder.Register<NetworkService>(Lifetime.Singleton);
            builder.Register<ServerTickDriver>(Lifetime.Singleton);
            builder.Register<NetworkTickDriver>(Lifetime.Singleton);
            builder.RegisterEntryPoint<CentralizedTickDriver>(Lifetime.Singleton);

            builder.RegisterEntryPoint<ServerWorldLoader>(Lifetime.Singleton);

            builder.RegisterBuildCallback(container =>
            {
                Debug.Log("Container built: " + container);
            });

            if (config.IsDebugWorld && debugObjectPrefab != null)
                builder.RegisterInstance(debugObjectPrefab);
        }
    }
}
