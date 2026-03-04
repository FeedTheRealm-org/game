using FTR.Core.Common.Config;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class ServerInitiator : LifetimeScope
{
    [SerializeField]
    private SceneReference mainScene;

    [SerializeField]
    private Config config;

    [SerializeField]
    private Logging.Logger logger;

    protected override void Configure(IContainerBuilder builder)
    {
        if (config.RuntimeRole != RuntimeRole.Server)
            throw new System.InvalidOperationException("Invalid runtime role for ServerInitiator");

        builder.RegisterInstance(mainScene).As<SceneReference>();
        builder.RegisterEntryPoint<ServerEntryPoint>();

        logger?.Log("ServerInitiator: Registered server entrypoint", this);
    }
}
