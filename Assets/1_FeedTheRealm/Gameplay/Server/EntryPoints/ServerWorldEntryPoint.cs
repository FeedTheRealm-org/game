using FTR.Core.Common.Scopes;
using FTR.Core.Server;
using FTR.Gameplay.Server.Loaders;
using FTR.Gameplay.Server.Scopes;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class ServerWorldEntryPoint : IStartable, ITickable
{
    private readonly ServerTickDriver serverTickDriver;
    private readonly NetworkTickDriver networkTickDriver;

    //private readonly ServerWorldLoader worldLoader;

    private readonly float tickStep = 1f / 30f;
    private float accumulator;

    public ServerWorldEntryPoint(
        ServerTickDriver serverTickDriver,
        NetworkTickDriver networkTickDriver,
        IObjectResolver resolver,
        ObjectResolverContainer resolverContainer,
        ServerWorldLoader worldLoader
    )
    {
        this.serverTickDriver = serverTickDriver;
        this.networkTickDriver = networkTickDriver;
        this.worldLoader = worldLoader;
        resolverContainer.SetResolver(resolver);
    }

    public void Start()
    {
        //worldLoader.LoadWorld();
    }

    /// <summary>
    /// Tick method is called by the VContainer's TickableManager every frame
    /// (60 TPS or as stated in server entrypoint), and it will call ServerTickDriver and NetworkTickDriver.
    /// </summary>
    public void Tick()
    {
        networkTickDriver.TickBefore();

        accumulator += Time.deltaTime;

        if (accumulator >= tickStep)
        {
            serverTickDriver.TickOnce(tickStep);
            accumulator -= tickStep;

            // Prevent spiral of death by ticking once per tickStep and not catching up
            if (accumulator > tickStep)
                accumulator = tickStep;
        }

        networkTickDriver.TickAfter();
    }
}
