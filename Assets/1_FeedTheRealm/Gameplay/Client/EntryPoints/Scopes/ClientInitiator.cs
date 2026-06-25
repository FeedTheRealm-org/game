using API;
using FeedTheRealm.Core.Client.EventChannels;
using FeedTheRealm.Gameplay.Client.SceneSetup;
using FTR.Core.Client.EntryPoints;
using FTR.Core.Client.Managers;
using FTR.Core.Client.Settings;
using FTR.Core.Common.Config;
using FTR.Gameplay.Client.EntryPoints;
using FTR.Gameplay.Client.Registry;
using FTRShared.Runtime.Core.Cache;
using FTRShared.Runtime.Core.Interfaces;
using FTRShared.UI.AuthMenu;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class ClientInitiator : LifetimeScope
{
    [SerializeField]
    private SceneReference mainScene;

    [SerializeField]
    private Session.Session session;

    [SerializeField]
    private Config config;

    [SerializeField]
    private WorldSelector worldSelector;

    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private SettingsManager settingsManager;

    [Header("Auth Prefabs")]
    [SerializeField]
    private AuthFlowManager authFlowManager;

    [SerializeField]
    private GameObject authBackgroundPrefab;

    [Header("Main Menu Prefabs")]
    [SerializeField]
    private GameObject worldFeedMenuPrefab;

    [SerializeField]
    private GameObject worldInfoMenuPrefab;

    [SerializeField]
    private GameObject navBarPrefab;

    [SerializeField]
    private GameObject profileMenuPrefab;

    [SerializeField]
    private GameObject gemStorePrefab;

    [SerializeField]
    private GameObject navBarSettingsPrefab;

    [SerializeField]
    private GameObject confirmPopupPrefab;

    [SerializeField]
    private GameObject downloadContentPopupPrefab;

    [Header("Loading Screen")]
    [SerializeField]
    private GameObject loadingScreenPrefab;

    [SerializeField]
    private API.AuthService authService;

    [Header("Audio")]
    [SerializeField]
    private GameObject musicPlayerPrefab;

    [SerializeField]
    private ClientMusicRegistry musicRegistry;

    [SerializeField]
    private ClientSoundFXRegistry soundFXRegistry;

    [Header("Player")]
    [SerializeField]
    private API.PlayerService playerService;

    [SerializeField]
    private API.AssetsService assetsService;

    [Header("Assets")]
    [SerializeField]
    private API.ModelService modelService;

    [SerializeField]
    private GltLoaderService gltfLoaderService;

    [SerializeField]
    private API.ExportsService exportService;

    [Header("Events")]
    [SerializeField]
    private ClientEventRegistry eventRegistry;

    protected override void Configure(IContainerBuilder builder)
    {
        if (config.RuntimeRole != RuntimeRole.Client)
            throw new System.InvalidOperationException("Invalid runtime role for ClientInitiator");

        ValidateSerializeFields();

        builder.RegisterInstance(session);
        builder.RegisterInstance(authService);
        builder.RegisterInstance(config);
        builder.RegisterInstance(worldSelector);
        builder.RegisterInstance(musicRegistry);
        builder.RegisterInstance(settingsManager);
        builder.RegisterInstance(playerService);
        builder.RegisterInstance(assetsService);
        builder.RegisterInstance(modelService);
        builder.RegisterInstance(soundFXRegistry);
        builder.RegisterInstance(gltfLoaderService).As<IGltfLoader>().AsSelf();
        builder.RegisterInstance(exportService);
        builder.RegisterInstance(logger);
        builder.Register<WorldInfoMenuHandle>(Lifetime.Singleton);
        eventRegistry.RegisterAll(builder);
        builder.Register<PlayerInfoRepository>(Lifetime.Singleton).As<CharacterInfoRepository>();
        builder.Register<SoundPlayer>(Lifetime.Singleton).As<ISoundPlayer>();
        builder.RegisterComponent(GetComponent<AudioManager>()).As<IAudioManager>();
        builder
            .RegisterEntryPoint<ClientEntryPoint>()
            .WithParameter("mainScene", mainScene)
            .WithParameter("authBackgroundPrefab", authBackgroundPrefab)
            .WithParameter("worldFeedMenuPrefab", worldFeedMenuPrefab)
            .WithParameter("worldInfoMenuPrefab", worldInfoMenuPrefab)
            .WithParameter("navBarPrefab", navBarPrefab)
            .WithParameter("profileMenuPrefab", profileMenuPrefab)
            .WithParameter("gemStorePrefab", gemStorePrefab)
            .WithParameter("navBarSettingsPrefab", navBarSettingsPrefab)
            .WithParameter("musicPlayerPrefab", musicPlayerPrefab)
            .WithParameter("musicRegistry", musicRegistry)
            .WithParameter("loadingScreenPrefab", loadingScreenPrefab)
            .WithParameter("settingsManager", settingsManager)
            .WithParameter("playerService", playerService)
            .WithParameter("assetsService", assetsService)
            .WithParameter("soundFXRegistry", soundFXRegistry)
            .WithParameter("onProfileCreatedEvent", eventRegistry.onProfileCreatedEvent)
            .WithParameter("onLogoutRequestedEvent", eventRegistry.onLogoutRequestedEvent)
            .WithParameter("confirmPopupPrefab", confirmPopupPrefab)
            .WithParameter("downloadContentPopupPrefab", downloadContentPopupPrefab);

        builder.RegisterComponentInNewPrefab(authFlowManager, Lifetime.Singleton);
        builder.Register<MenuManager>(Lifetime.Singleton);
        builder.Register<CursorManager>(Lifetime.Singleton);
        builder.Register<CameraManager>(Lifetime.Singleton);
        builder.Register<ConfirmPopupHandle>(Lifetime.Singleton);
        builder.Register<CacheManager>(Lifetime.Singleton);
        builder.Register<DiskService>(Lifetime.Singleton);
        logger?.Log("ClientInitiator: Registered client entrypoint", this);
    }

    private void ValidateSerializeFields()
    {
        ValidateField(config, "Config");
        ValidateField(worldSelector, "WorldSelector");
        ValidateField(logger, "Logger");
        ValidateField(session, "Session");
        ValidateField(settingsManager, "SettingsManager");
        ValidateField(musicRegistry, "MusicRegistry");
        ValidateField(assetsService, "AssetsService");
        ValidateField(modelService, "ModelService");
        ValidateField(gltfLoaderService, "GLTFLoaderService");
        ValidateField(exportService, "ExportService");
        ValidateField(soundFXRegistry, "SoundFXRegistry");
        ValidateField(authFlowManager, "AuthFlowManager");
        ValidateField(authBackgroundPrefab, "AuthBackgroundPrefab");
        ValidateField(worldFeedMenuPrefab, "WorldFeedMenuPrefab");
        ValidateField(worldInfoMenuPrefab, "WorldInfoMenuPrefab");
        ValidateField(navBarPrefab, "NavBarPrefab");
        ValidateField(profileMenuPrefab, "ProfileMenuPrefab");
        ValidateField(gemStorePrefab, "GemStorePrefab");
        ValidateField(navBarSettingsPrefab, "NavBarSettingsPrefab");
        ValidateField(confirmPopupPrefab, "ConfirmPopupPrefab");
        ValidateField(loadingScreenPrefab, "LoadingScreenPrefab");
        ValidateField(musicPlayerPrefab, "MusicPlayerPrefab");
        ValidateField(playerService, "PlayerService");
    }

    private void ValidateField(object field, string fieldName)
    {
        if (field == null)
            throw new System.NullReferenceException(
                $"{fieldName} is not assigned in the Inspector."
            );
    }
}
