using FTR.Core.Client.EventChannels.Ticks;
using VContainer;
using VContainer.Unity;

public class ClientWorldEntryPoint : IStartable, ITickable, IFixedTickable, ILateTickable
{
    private TickEvent tickEvent;

    private FixedTickEvent fixedTickEvent;

    private LateTickEvent lateTickEvent;

    public void ClientWorldEntrypoint(
        TickEvent tickEvent,
        FixedTickEvent fixedTickEvent,
        LateTickEvent lateTickEvent
    )
    {
        this.tickEvent = tickEvent;
        this.fixedTickEvent = fixedTickEvent;
        this.lateTickEvent = lateTickEvent;
    }

    public void Start()
    {
        // Initialize client world here
    }

    public void Tick()
    {
        tickEvent.Raise();
    }

    public void FixedTick()
    {
        fixedTickEvent.Raise();
    }

    public void LateTick()
    {
        lateTickEvent.Raise();
    }
}
