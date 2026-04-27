using FeedTheRealm.Gameplay.Client.SceneSetup;
using FTR.Core.Client;
using FTR.Core.Client.EventChannels.Ticks;
using FTR.Core.Common.Scopes;
using FTR.Gameplay.Client.Loaders;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.EntryPoints
{
    /// <summary>
    /// Entry point for the client application, responsible for initializing the application flow, including authentication and main menu navigation.
    /// </summary>
    public class ClientWorldEntryPoint : IStartable, ITickable, IFixedTickable, ILateTickable
    {
        private TickEvent tickEvent;

        private FixedTickEvent fixedTickEvent;

        private LateTickEvent lateTickEvent;

        private bool isInitialized = false;

        private readonly ClientWorldLoader worldLoader;
        private readonly WorldSetupService worldSetup;

        [Inject]
        public ClientWorldEntryPoint(
            TickEvent tickEvent,
            FixedTickEvent fixedTickEvent,
            LateTickEvent lateTickEvent,
            IObjectResolver resolver,
            ObjectResolverContainer resolverContainer,
            ClientWorldLoader worldLoader,
            WorldSetupService worldSetup
        )
        {
            this.tickEvent = tickEvent;
            this.fixedTickEvent = fixedTickEvent;
            this.lateTickEvent = lateTickEvent;
            resolverContainer.SetResolver(resolver);
            this.worldLoader = worldLoader;
            this.worldSetup = worldSetup;
            isInitialized = true;
        }

        public async void Start()
        {
            await worldLoader.LoadWorld();
            worldSetup.ExecuteSetup();
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
}
