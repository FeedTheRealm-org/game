using UnityEngine;
using VContainer.Unity;

public sealed class CentralizedTickDriver : ITickable
{
    private readonly ServerTickDriver serverTickDriver;
    private readonly NetworkTickDriver networkTickDriver;

    private readonly float tickStep = 1f / 30f;
    private float accumulator;

    public CentralizedTickDriver(
        ServerTickDriver serverTickDriver,
        NetworkTickDriver networkTickDriver
    )
    {
        this.serverTickDriver = serverTickDriver;
        this.networkTickDriver = networkTickDriver;
    }

    /// <summary>
    /// Tick method is called by the VContainer's TickableManager every frame
    /// (60 TPS or as stated in server entrypoint), and it will call ServerTickDriver and NetworkTickDriver.
    /// </summary
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
