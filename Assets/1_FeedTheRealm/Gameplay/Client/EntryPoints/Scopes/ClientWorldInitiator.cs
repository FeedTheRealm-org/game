using API;
using FTR.Core.Client;
using FTR.Core.Common.Config;
using FTR.Core.Common.Loaders;
using FTR.Gameplay.Client.Characters;
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
    private Logging.Logger logger;

    protected override void Configure(IContainerBuilder builder)
    {
        if (config.RuntimeRole != RuntimeRole.Client)
            return;

        builder.RegisterInstance(playerInputReader);
        builder.RegisterInstance(prefabProvider);
        builder.Register<ClientCharacterLinker>(Lifetime.Singleton).As<IScriptLinker>();
        builder.Register<GltLoaderService>(Lifetime.Singleton);
        builder.RegisterEntryPoint<ClientWorldEntryPoint>();

        logger?.Log("WorldInitiator: Registered as Client", this);
    }
}
