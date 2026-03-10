using API;
using FTR.Core.Client.EventChannels.Ticks;
using FTR.Core.Common.Scopes;
using FTR.Gameplay.Common.LoaderEntities;
using FTR.Gameplay.Common.WorldLoader;
using Logging;
using VContainer;
using VContainer.Unity;

public class ClientWorldEntryPoint : WorldLoader, ITickable, IFixedTickable, ILateTickable
{
    private TickEvent tickEvent;

    private FixedTickEvent fixedTickEvent;

    private LateTickEvent lateTickEvent;

    private bool isInitialized = false;

    private Session.Session session;

    private WorldSelector worldSelector;

    [Inject]
    public ClientWorldEntryPoint(
        TickEvent tickEvent,
        FixedTickEvent fixedTickEvent,
        LateTickEvent lateTickEvent,
        Session.Session session,
        WorldService worldService,
        Logger logger,
        LoaderProvider loaderProvider,
        WorldSelector worldSelector,
        IObjectResolver resolver,
        ObjectResolverContainer resolverContainer
    )
        : base(worldService, logger, loaderProvider)
    {
        this.tickEvent = tickEvent;
        this.fixedTickEvent = fixedTickEvent;
        this.lateTickEvent = lateTickEvent;
        this.session = session;
        this.worldSelector = worldSelector;
        resolverContainer.SetResolver(resolver);
        isInitialized = true;
    }

    public override string GetWorldId()
    {
        return worldSelector.GetSelectedWorldId();
    }

    public override string GetAccessToken()
    {
        return session.APIToken;
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
