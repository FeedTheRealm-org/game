using FeedTheRealm.Core.Client.EventChannels;
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
    private GameObject navBarPrefab;

    [SerializeField]
    private GameObject profileMenuPrefab;

    [SerializeField]
    private GameObject gemStorePrefab;

    [SerializeField]
    private GameObject navBarSettingsPrefab;

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
        eventRegistry.RegisterAll(builder);
        builder.Register<PlayerInfoRepository>(Lifetime.Singleton).As<CharacterInfoRepository>();
        builder
            .RegisterEntryPoint<ClientEntryPoint>()
            .WithParameter("mainScene", mainScene)
            .WithParameter("authBackgroundPrefab", authBackgroundPrefab)
            .WithParameter("worldFeedMenuPrefab", worldFeedMenuPrefab)
            .WithParameter("navBarPrefab", navBarPrefab)
            .WithParameter("profileMenuPrefab", profileMenuPrefab)
            .WithParameter("gemStorePrefab", gemStorePrefab)
            .WithParameter("navBarSettingsPrefab", navBarSettingsPrefab)
            .WithParameter("musicPlayerPrefab", musicPlayerPrefab)
            .WithParameter("musicRegistry", musicRegistry)
            .WithParameter("loadingScreenPrefab", loadingScreenPrefab)
            .WithParameter("settingsManager", settingsManager)
            .WithParameter("playerService", playerService)
            .WithParameter("onProfileCreatedEvent", eventRegistry.onProfileCreatedEvent);

        builder.RegisterComponentInNewPrefab(authFlowManager, Lifetime.Singleton);
        logger?.Log("ClientInitiator: Registered client entrypoint", this);
    }
}
