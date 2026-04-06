using FTR.Core.Common.Config;
using FTR.Core.Common.EventChannels;
using FTR.Core.Common.Scopes;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class WorldInitiator : LifetimeScope
{
    [SerializeField]
    private CommonEventRegistry commonEventRegistry;

    [SerializeField]
    private ObjectResolverContainer objectResolverContainer;

    [SerializeField]
    private Config config;

    [SerializeField]
    private Logging.Logger logger;

    protected override void Configure(IContainerBuilder builder)
    {
        commonEventRegistry.RegisterAll(builder);
        builder.RegisterInstance(objectResolverContainer);
        builder.RegisterInstance(config);
        builder.RegisterInstance(logger); // Default logger

        logger?.Log("WorldInitiator: Registered as Common", this);
    }
}
