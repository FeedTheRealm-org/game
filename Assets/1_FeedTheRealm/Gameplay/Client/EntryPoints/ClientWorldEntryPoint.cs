using FTR.Core.Client.EventChannels.Ticks;
using VContainer;
using VContainer.Unity;

public class ClientWorldEntryPoint : IStartable, ITickable, IFixedTickable, ILateTickable
{
    private TickEvent tickEvent;

    private FixedTickEvent fixedTickEvent;

    private LateTickEvent lateTickEvent;

    private bool isInitialized = false;

    public void ClientWorldEntrypoint(
        TickEvent tickEvent,
        FixedTickEvent fixedTickEvent,
        LateTickEvent lateTickEvent
    )
    {
        this.tickEvent = tickEvent;
        this.fixedTickEvent = fixedTickEvent;
        this.lateTickEvent = lateTickEvent;
        isInitialized = true;
    }

    public void Start()
    {
        // Initialize client world here
    }

    public void Tick()
    {
        if (!isInitialized)
            return;
        tickEvent.Raise();
    }

    public void FixedTick()
    {
        if (!isInitialized)
            return;
        fixedTickEvent.Raise();
    }

    public void LateTick()
    {
        if (!isInitialized)
            return;
        lateTickEvent.Raise();
    }
}
