using Mirror;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class WorldInitiator : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Shared systems (exist on both)
        // builder.Register<WorldState>(Lifetime.Singleton);
        // builder.Register<Clock>(Lifetime.Singleton);

        if (NetworkServer.active)
        {
            RegisterServer(builder);
        }
        else
        {
            RegisterClient(builder);
        }
    }

    void RegisterServer(IContainerBuilder builder)
    {
        builder.Register<GameLoop>(Lifetime.Singleton);
        builder.Register<NetworkService>(Lifetime.Singleton);

        builder.Register<ServerTickDriver>(Lifetime.Singleton);
        builder.Register<NetworkTickDriver>(Lifetime.Singleton);
        builder.RegisterEntryPoint<CentralizedTickDriver>();
    }

    void RegisterClient(IContainerBuilder builder)
    {
        // builder.Register<ClientPresentation>(Lifetime.Singleton);
        // builder.Register<ClientInput>(Lifetime.Singleton);

        // builder.RegisterEntryPoint<ClientUpdateDriver>();
    }
}
