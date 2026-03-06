using API;
using FTR.Core.Client;
using FTR.Core.Client.EventChannels.Ticks;
using FTR.Core.Common.Config;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Client.Characters;
using FTR.Gameplay.Common.WorldLoader;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class ClientWorldInitiator : LifetimeScope
{
    [SerializeField]
    private Config config;

    [SerializeField]
    private PlayerInputReader playerInputReader;

    [SerializeField]
    private ClientPrefabProvider prefabProvider;

    [SerializeField]
    private TickEvent tickEvent;

    [SerializeField]
    private FixedTickEvent fixedTickEvent;

    [SerializeField]
    private LateTickEvent lateTickEvent;

    [SerializeField]
    private Logging.Logger logger;

    [SerializeField]
    private WorldSelector worldSelector;

    [SerializeField]
    private WorldService worldService;

    [SerializeField]
    private LoaderProvider loaderProvider;

    [SerializeField]
    private Session.Session session;

    protected override void Configure(IContainerBuilder builder)
    {
        if (config.RuntimeRole != RuntimeRole.Client)
            return;

        builder.RegisterInstance(playerInputReader);
        builder.RegisterInstance(prefabProvider);
        builder.RegisterInstance(tickEvent);
        builder.RegisterInstance(fixedTickEvent);
        builder.RegisterInstance(lateTickEvent);
        builder.RegisterInstance(logger);
        builder.RegisterInstance(worldSelector);
        builder.RegisterInstance(worldService);
        builder.RegisterInstance(loaderProvider);
        builder.RegisterInstance(session);
        builder.Register<ClientCharacterLinker>(Lifetime.Singleton).As<IScriptLinker>();

        builder.RegisterEntryPoint<ClientWorldEntryPoint>();
    }
}
