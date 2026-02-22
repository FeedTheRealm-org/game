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
    private Logging.Logger logger;

    protected override void Configure(IContainerBuilder builder)
    {
        if (config.RuntimeRole != RuntimeRole.Client)
            return;

        builder.Register<ClientCharacterLinker>(Lifetime.Singleton).As<IScriptLinker>();

        logger?.Log("WorldInitiator: Registered as Client", this);
    }
}
