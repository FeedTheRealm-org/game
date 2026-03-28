using API;
using FeedTheRealm.Core.Client.EventChannels;
using FeedTheRealm.Gameplay.Client.SceneSetup;
using FTR.Core.Client;
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

            Validate();

            clientEventRegistry.RegisterAll(builder);
            setupServices.RegisterAll(builder);
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

        private void Validate()
        {
            ValidateField(config, nameof(config));
            ValidateField(playerInputReader, nameof(playerInputReader));
            ValidateField(prefabProvider, nameof(prefabProvider));
            ValidateField(clientEventRegistry, nameof(clientEventRegistry));
            ValidateField(logger, nameof(logger));
            ValidateField(session, nameof(session));
            ValidateField(worldSelector, nameof(worldSelector));
            ValidateField(worldService, nameof(worldService));
            ValidateField(npcDialogRegistry, nameof(npcDialogRegistry));
        }

        private void ValidateField(Object field, string fieldName)
        {
            if (field == null)
                throw new System.Exception($"[ClientWorldInitiator] {fieldName} is not assigned.");
        }
    }
}
