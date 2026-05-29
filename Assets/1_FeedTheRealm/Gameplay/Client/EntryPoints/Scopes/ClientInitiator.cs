using FeedTheRealm.Core.Client.EventChannels;
using FeedTheRealm.Gameplay.Client.SceneSetup;
using FTR.Core.Client.Managers;
using FTR.Core.Client.Settings;
using FTR.Core.Common.Config;
using FTR.Gameplay.Client.EntryPoints;
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

    [Header("Player")]
    [SerializeField]
    private API.PlayerService playerService;

    [SerializeField]
    private API.AssetsService assetsService;

    [Header("Assets")]
    [SerializeField]
    private API.ModelService modelService;

    [Header("Events")]
    [SerializeField]
    private ClientEventRegistry eventRegistry;

    protected override void Configure(IContainerBuilder builder)
    {
        if (config.RuntimeRole != RuntimeRole.Client)
            throw new System.InvalidOperationException("Invalid runtime role for ClientInitiator");

        builder.RegisterInstance(session);
        builder.RegisterInstance(authService);
        builder.RegisterInstance(musicRegistry);
        builder.RegisterInstance(settingsManager);
        builder.RegisterInstance(playerService);
        builder.RegisterInstance(assetsService);
        builder.RegisterInstance(logger);
        builder.RegisterInstance(modelService);
        builder.Register<WorldInfoMenuHandle>(Lifetime.Singleton);
        eventRegistry.RegisterAll(builder);
        builder.Register<PlayerInfoRepository>(Lifetime.Singleton).As<CharacterInfoRepository>();
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
            .WithParameter("onProfileCreatedEvent", eventRegistry.onProfileCreatedEvent)
            .WithParameter("onLogoutRequestedEvent", eventRegistry.onLogoutRequestedEvent)
            .WithParameter("confirmPopupPrefab", confirmPopupPrefab);

        builder.RegisterComponentInNewPrefab(authFlowManager, Lifetime.Singleton);
        builder.Register<MenuManager>(Lifetime.Singleton);
        builder.Register<CursorManager>(Lifetime.Singleton);
        builder.Register<CameraManager>(Lifetime.Singleton);
        builder.Register<ConfirmPopupHandle>(Lifetime.Singleton);
        logger?.Log("ClientInitiator: Registered client entrypoint", this);
    }
}
