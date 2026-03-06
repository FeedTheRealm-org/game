using FTR.Core.Common.Config;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class ClientInitiator : LifetimeScope
{
    [SerializeField]
    private SceneReference mainScene;

    [SerializeField]
    private Config config;

    [SerializeField]
    private Logging.Logger logger;

    protected override void Configure(IContainerBuilder builder)
    {
        if (config.RuntimeRole != RuntimeRole.Client)
            throw new System.InvalidOperationException("Invalid runtime role for ClientInitiator");

        builder.RegisterInstance(mainScene).As<SceneReference>();
        builder.RegisterEntryPoint<ClientEntryPoint>();

        logger?.Log("ClientInitiator: Registered client entrypoint", this);
    }
}
