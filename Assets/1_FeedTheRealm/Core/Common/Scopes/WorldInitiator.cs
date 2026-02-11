using FTR.Core.Common.Config;
using FTR.Core.Common.EventChannels;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class WorldInitiator : LifetimeScope
{
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
        builder.RegisterInstance(receivedActionCommandEvent);
        builder.RegisterInstance(receivedTransactionCommandEvent);

        logger?.Log("WorldInitiator: Registered as Common", this);
    }
}
