using API;
using FTR.Core.Common.Config;
using FTR.Core.Server;
using FTR.Core.Server.Config;
using FTR.Core.Server.EventChannels;
using FTR.Core.Server.Healthcheck;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Common.Linkers;
using FTR.Gameplay.Server.Environment.Quest;
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
        private ServerConfig serverConfig;

        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private ServerEventRegistry serverEventRegistry;

        [SerializeField]
        private ServerPrefabProvider prefabProvider;

        [SerializeField]
        private NpcDialogRegistry npcDialogRegistry;

        [SerializeField]
        private ServerQuestRegistry serverQuestRegistry;

        [SerializeField]
        private WorldService worldService;

        [SerializeField]
        private ZoneService zoneService;

        protected override void Configure(IContainerBuilder builder)
        {
            if (config.RuntimeRole != RuntimeRole.Server)
                return;

            Validate();

            serverEventRegistry.RegisterAll(builder);

            builder.RegisterInstance(serverConfig);
            builder.RegisterInstance(logger);
            builder.RegisterInstance(prefabProvider);
            builder.RegisterInstance(worldService);
            builder.RegisterInstance(zoneService);
            builder.RegisterInstance(npcDialogRegistry);
            builder.RegisterInstance(serverQuestRegistry);

            builder.Register<HealthcheckServer>(Lifetime.Singleton);
            builder.Register<PlayerSpawnpointManager>(Lifetime.Singleton);
            builder.Register<ServerWorldLoader>(Lifetime.Singleton);
            builder.Register<WorldMonitor>(Lifetime.Singleton);
            builder.Register<ServerPlayerLinker>(Lifetime.Singleton).As<PlayerLinker>();
            builder.Register<ServerAggresiveNpcLinker>(Lifetime.Singleton).As<AggresiveNpcLinker>();
            builder.Register<ServerPassiveNpcLinker>(Lifetime.Singleton).As<PassiveNpcLinker>();
            builder.Register<ServerLootItemLinker>(Lifetime.Singleton).As<LootItemLinker>();
            builder.Register<ServerShopLinker>(Lifetime.Singleton).As<ShopLinker>();

            builder.Register<GameLoop>(Lifetime.Singleton);
            builder.Register<NetworkService>(Lifetime.Singleton);
            builder.Register<ServerTickDriver>(Lifetime.Singleton);
            builder.Register<NetworkTickDriver>(Lifetime.Singleton);

            builder.RegisterEntryPoint<ServerWorldEntryPoint>(Lifetime.Singleton);
        }

        private void Validate()
        {
            ValidateField(config, nameof(config));
            ValidateField(logger, nameof(logger));
            ValidateField(serverEventRegistry, nameof(serverEventRegistry));
            ValidateField(prefabProvider, nameof(prefabProvider));
            ValidateField(worldService, nameof(worldService));
            ValidateField(npcDialogRegistry, nameof(npcDialogRegistry));
            ValidateField(serverQuestRegistry, nameof(serverQuestRegistry));
        }

        private void ValidateField(Object field, string fieldName)
        {
            if (field == null)
                throw new System.Exception($"[ServerWorldInitiator] {fieldName} is not assigned.");
        }
    }
}
