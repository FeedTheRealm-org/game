using API;
using FeedTheRealm.Core.Client.EventChannels;
using FeedTheRealm.Gameplay.Client.SceneSetup;
using FTR.Core.Client;
using FTR.Core.Client.Config;
using FTR.Core.Common.Config;
using FTR.Gameplay.Client.Linkers;
using FTR.Gameplay.Client.Loaders;
using FTR.Gameplay.Common.Environment.Dialogs;
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
        private ClientConfig clientConfig;

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
        private NpcDialogRegistry npcDialogRegistry;

        [SerializeField]
        private ModelService modelService;

        [SerializeField]
        private GltLoaderService gltfLoaderService;

        private readonly SetupServices setupServices = new();

        protected override void Configure(IContainerBuilder builder)
        {
            if (config.RuntimeRole != RuntimeRole.Client)
                return;

            ValidateSerializeFields();

            clientEventRegistry.RegisterAll(builder);
            setupServices.RegisterAll(builder);
            builder.RegisterInstance(clientConfig);
            builder.RegisterInstance(playerInputReader);
            builder.RegisterInstance(prefabProvider);
            builder.RegisterInstance(logger);
            builder.RegisterInstance(worldSelector);
            builder.RegisterInstance(worldService);
            builder.RegisterInstance(config);
            builder.RegisterInstance(session);
            builder.RegisterInstance(npcDialogRegistry);
            builder.RegisterInstance(modelService);
            builder.RegisterInstance(gltfLoaderService);
            builder.Register<ClientWorldLoader>(Lifetime.Singleton);
            builder.Register<ClientPlayerLinker>(Lifetime.Singleton).As<PlayerLinker>();
            builder.Register<ClientAggresiveNpcLinker>(Lifetime.Singleton).As<AggresiveNpcLinker>();
            builder.Register<ClientPassiveNpcLinker>(Lifetime.Singleton).As<PassiveNpcLinker>();
            builder.Register<ClientLootItemLinker>(Lifetime.Singleton).As<LootItemLinker>();

            builder.RegisterEntryPoint<ClientWorldEntryPoint>();
        }

        private void ValidateSerializeFields()
        {
            ValidateField(config, "Config");
            ValidateField(playerInputReader, "PlayerInputReader");
            ValidateField(prefabProvider, "PrefabProvider");
            ValidateField(clientEventRegistry, "ClientEventRegistry");
            ValidateField(logger, "Logger");
            ValidateField(worldSelector, "WorldSelector");
            ValidateField(worldService, "WorldService");
            ValidateField(config, "Config");
            ValidateField(session, "Session");
            ValidateField(modelService, "ModelService");
            ValidateField(gltfLoaderService, "GLTFLoaderService");
        }

        private void ValidateField(object field, string fieldName)
        {
            if (field == null)
                throw new System.NullReferenceException(
                    $"{fieldName} is not assigned in the Inspector."
                );
        }
    }
}
