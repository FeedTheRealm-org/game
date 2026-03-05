using API;
using FTR.Core.Common.Config;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Loaders;
using FTR.Core.Server;
using FTR.Core.Server.EventChannels;
using FTR.Gameplay.Server.Characters;
using FTR.Gameplay.Server.WorldLoader;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class ServerWorldInitiator : LifetimeScope
{
    [SerializeField]
    private Config config;

    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private GameTickEvent gameTickEvent;

    [SerializeField]
    private WorldReadyEvent worldReadyEvent;

    [SerializeField]
    private ServerPrefabProvider prefabProvider;

    [Header("Services")]
    [SerializeField]
    private WorldService worldService;

    protected override void Configure(IContainerBuilder builder)
    {
        if (config.RuntimeRole != RuntimeRole.Server)
            return;

        builder.Register<WorldMonitor>(Lifetime.Singleton);
        builder.RegisterInstance(prefabProvider);
        builder.Register<ServerCharacterLinker>(Lifetime.Singleton).As<IScriptLinker>();
        builder.RegisterInstance(gameTickEvent);
        builder.Register<GameLoop>(Lifetime.Singleton);
        builder.Register<NetworkService>(Lifetime.Singleton);

        builder.Register<ServerTickDriver>(Lifetime.Singleton);
        builder.Register<NetworkTickDriver>(Lifetime.Singleton);
        builder.RegisterEntryPoint<CentralizedTickDriver>(Lifetime.Singleton);

        builder.RegisterInstance(worldReadyEvent);
        builder.RegisterInstance(worldService);
        builder.RegisterEntryPoint<ServerWorldLoader>(Lifetime.Singleton);

        logger?.Log("WorldInitiator: Registered as Server", this);
    }
}
