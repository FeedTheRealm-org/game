using FTR.Core.Common.Config;
using FTR.Core.Common.Loaders;
using FTR.Core.Server.EventChannels;
using FTR.Gameplay.Server.Characters;
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

    protected override void Configure(IContainerBuilder builder)
    {
        if (config.RuntimeRole != RuntimeRole.Server)
            return;

        builder.Register<WorldMonitor>(Lifetime.Singleton);
        builder.Register<ServerCharacterLinker>(Lifetime.Singleton).As<IScriptLinker>();
        builder.RegisterInstance(gameTickEvent);
        builder.Register<GameLoop>(Lifetime.Singleton);
        builder.Register<NetworkService>(Lifetime.Singleton);

        builder.Register<ServerTickDriver>(Lifetime.Singleton);
        builder.Register<NetworkTickDriver>(Lifetime.Singleton);
        builder.RegisterEntryPoint<CentralizedTickDriver>();

        logger?.Log("WorldInitiator: Registered as Server", this);
    }
}
