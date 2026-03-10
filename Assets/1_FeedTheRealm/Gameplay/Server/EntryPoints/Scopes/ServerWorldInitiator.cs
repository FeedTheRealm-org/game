using API;
using FTR.Core.Common.Config;
using FTR.Core.Common.Enums;
using FTR.Core.Common.Loaders;
using FTR.Core.Server;
using FTR.Core.Server.EventChannels;
using FTR.Gameplay.Common.WorldLoader;
using FTR.Gameplay.Server.Characters;
using FTR.Gameplay.Server.Environment.LootItem;
using FTR.Gameplay.Server.Scopes;
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
    private ServerPrefabProvider prefabProvider;

    [SerializeField]
    private LoaderProvider loaderProvider;

    [SerializeField]
    private WorldService worldService;

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

        builder
            .Register<IScriptLinker, ServerCharacterLinker>(Lifetime.Singleton)
            .Keyed(RegisterTypes.Character);
        builder
            .Register<IScriptLinker, ServerLootItemLinker>(Lifetime.Singleton)
            .Keyed(RegisterTypes.LootItem);

        builder.Register<GameLoop>(Lifetime.Singleton);
        builder.Register<NetworkService>(Lifetime.Singleton);
        builder.Register<ServerTickDriver>(Lifetime.Singleton);
        builder.Register<NetworkTickDriver>(Lifetime.Singleton);
        builder.RegisterEntryPoint<CentralizedTickDriver>(Lifetime.Singleton);

        builder.RegisterEntryPoint<ServerWorldLoader>(Lifetime.Singleton);
    }
}
