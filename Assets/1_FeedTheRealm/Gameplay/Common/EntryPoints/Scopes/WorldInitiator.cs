using FTR.Core.Common.Config;
using FTR.Core.Common.EventChannels;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class WorldInitiator : LifetimeScope
{
    [SerializeField]
    private CommonEventRegistry commonEventRegistry;

    [SerializeField]
    private ReceivedActionCommandEvent receivedActionCommandEvent;

    [SerializeField]
    private ReceivedTransactionCommandEvent receivedTransactionCommandEvent;

    [SerializeField]
    private Config config;

    [SerializeField]
    private Logging.Logger logger;

    protected override void Configure(IContainerBuilder builder)
    {
        commonEventRegistry.RegisterAll(builder);
        builder.RegisterInstance(config);
        builder.RegisterInstance(logger); // Default logger
        builder.RegisterInstance(receivedActionCommandEvent);
        builder.RegisterInstance(receivedTransactionCommandEvent);

        logger?.Log("WorldInitiator: Registered as Common", this);
    }
}
