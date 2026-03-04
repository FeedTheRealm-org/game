using FTR.Core.Client.EventChannels.Ticks;
using VContainer;
using VContainer.Unity;

public class ClientWorldEntryPoint : IStartable, ITickable, IFixedTickable, ILateTickable
{
    private TickEvent tickEvent;

    private FixedTickEvent fixedTickEvent;

    private LateTickEvent lateTickEvent;

    private bool isInitialized = false;

    [Inject]
    public ClientWorldEntryPoint(
        TickEvent tickEvent,
        FixedTickEvent fixedTickEvent,
        LateTickEvent lateTickEvent
    )
    {
        this.tickEvent = tickEvent;
        this.fixedTickEvent = fixedTickEvent;
        this.lateTickEvent = lateTickEvent;
        isInitialized = true;

        UnityEngine.Debug.Log("[ClientWorldEntryPoint] Constructor - Dependencies injected");
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
        UnityEngine.Debug.Log("[ClientWorldEntryPoint] FixedTick raised");
    }

    public void LateTick()
    {
        if (!isInitialized)
            return;
        lateTickEvent.Raise();
    }
}
