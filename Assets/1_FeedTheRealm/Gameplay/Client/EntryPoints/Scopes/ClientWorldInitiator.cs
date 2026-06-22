using API;
using FeedTheRealm.Core.Client.EventChannels;
using FeedTheRealm.Gameplay.Client.SceneSetup;
using FTR.Core.Client;
using FTR.Core.Client.Config;
using FTR.Core.Client.EntryPoints;
using FTR.Core.Client.Managers;
using FTR.Core.Client.Settings;
using FTR.Core.Common.Config;
using FTR.Gameplay.Client.Environment.Quest;
using FTR.Gameplay.Client.Linkers;
using FTR.Gameplay.Client.Loaders;
using FTR.Gameplay.Client.Registry;
using FTR.Gameplay.Common.Characters.Shared.Portal;
using FTR.Gameplay.Common.Environment.Dialogs;
using FTR.Gameplay.Common.Linkers;
using FTRShared.Runtime.Core.Cache;
using FTRShared.Runtime.Core.Interfaces;
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
        private ClientQuestRegistry clientQuestRegistry;

        [SerializeField]
        private Logging.Logger logger;

        [SerializeField]
        private Session.Session session;

        [SerializeField]
        private SettingsManager settingsManager;

        [SerializeField]
        private WorldSelector worldSelector;

        [SerializeField]
        private WorldService worldService;

        [SerializeField]
        private ZoneService zoneService;

        [SerializeField]
        private NpcDialogRegistry npcDialogRegistry;

        [SerializeField]
        private ModelService modelService;

        [SerializeField]
        private GltLoaderService gltfLoaderService;

        [SerializeField]
        private PlayerService playerService;

        [SerializeField]
        private AssetsService assetsService;

        [SerializeField]
        private MaterialService materialService;

        [SerializeField]
        private ColliderRegistry colliderRegistry;

        [SerializeField]
        private AudioManager audioManager;

        [SerializeField]
        private ClientSoundFXRegistry soundFXRegistry;

        [SerializeField]
        private ClientMusicRegistry musicRegistry;

        // TODO(portal): this is a temporary solution, we need to refactor
        // how we handle teleportation data on the client and remove
        // this dependency from the ClientInitiator
        [Header("Teleportation (TO REFACTOR LATER)")]
        [SerializeField]
        private TeleportDataPersistence teleportDataPersistence;

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
            builder.RegisterInstance(zoneService);
            builder.RegisterInstance(config);
            builder.RegisterInstance(session);
            builder.RegisterInstance(settingsManager);
            builder.RegisterInstance(npcDialogRegistry);
            builder.RegisterInstance(clientQuestRegistry);
            builder.RegisterInstance(modelService);
            builder.RegisterInstance(gltfLoaderService).As<IGltfLoader>().AsSelf();
            builder.RegisterInstance(playerService);
            builder.RegisterInstance(assetsService);
            builder.RegisterInstance(materialService);
            builder.RegisterInstance(colliderRegistry);
            builder.RegisterInstance(soundFXRegistry);
            builder.RegisterInstance(musicRegistry);
            builder.RegisterInstance(teleportDataPersistence);
            builder.RegisterComponent(audioManager).As<IAudioManager>();
            builder.Register<SoundPlayer>(Lifetime.Singleton).As<ISoundPlayer>();
            builder.Register<PlayerInfoRepository>(Lifetime.Singleton);
            builder.Register<ClientNpcInfoRepository>(Lifetime.Singleton);
            builder.Register<ClientWorldLoader>(Lifetime.Singleton);
            builder.Register<CursorManager>(Lifetime.Singleton);
            builder.Register<CameraManager>(Lifetime.Singleton);
            builder.Register<MenuManager>(Lifetime.Singleton);
            builder.Register<CacheManager>(Lifetime.Singleton);
            builder.Register<DiskService>(Lifetime.Singleton);
            builder.Register<INetworkStats, MirrorNetworkStats>(Lifetime.Singleton);
            builder.Register<ClientPlayerLinker>(Lifetime.Singleton).As<PlayerLinker>();
            builder.Register<ClientAggresiveNpcLinker>(Lifetime.Singleton).As<AggresiveNpcLinker>();
            builder.Register<ClientPassiveNpcLinker>(Lifetime.Singleton).As<PassiveNpcLinker>();
            builder.Register<ClientLootItemLinker>(Lifetime.Singleton).As<LootItemLinker>();
            builder.Register<ClientShopLinker>(Lifetime.Singleton).As<ShopLinker>();
            builder.Register<ClientPortalLinker>(Lifetime.Singleton).As<PortalLinker>();
            builder.Register<ClientChestLinker>(Lifetime.Singleton).As<ChestLinker>();

            builder.RegisterEntryPoint<ClientWorldEntryPoint>();
        }

        private void ValidateSerializeFields()
        {
            ValidateField(config, "Config");
            ValidateField(playerInputReader, "PlayerInputReader");
            ValidateField(prefabProvider, "PrefabProvider");
            ValidateField(clientEventRegistry, "ClientEventRegistry");
            ValidateField(clientQuestRegistry, "ClientQuestRegistry");
            ValidateField(logger, "Logger");
            ValidateField(worldSelector, "WorldSelector");
            ValidateField(worldService, "WorldService");
            ValidateField(session, "Session");
            ValidateField(settingsManager, "SettingsManager");
            ValidateField(modelService, "ModelService");
            ValidateField(gltfLoaderService, "GLTFLoaderService");
            ValidateField(assetsService, "AssetsService");
            ValidateField(zoneService, "ZoneService");
            ValidateField(npcDialogRegistry, "NpcDialogRegistry");
            ValidateField(materialService, "MaterialService");
            ValidateField(colliderRegistry, "ColliderRegistry");
            ValidateField(audioManager, "AudioManager");
            ValidateField(soundFXRegistry, "SoundFXRegistry");
            ValidateField(musicRegistry, "MusicRegistry");
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
