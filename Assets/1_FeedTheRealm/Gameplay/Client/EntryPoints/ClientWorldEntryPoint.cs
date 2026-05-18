using FeedTheRealm.Core.EventChannels.Setup;
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
        private readonly TickEvent tickEvent;
        private readonly FixedTickEvent fixedTickEvent;
        private readonly LateTickEvent lateTickEvent;
        private readonly LoadingEvent loadingEvent;
        private readonly ClientWorldLoader worldLoader;
        private readonly WorldSetupService worldSetup;
        private readonly ClientPrefabProvider prefabProvider;
        private readonly ClientMusicRegistry musicRegistry;

        private bool isInitialized = false;

        [Inject]
        public ClientWorldEntryPoint(
            TickEvent tickEvent,
            FixedTickEvent fixedTickEvent,
            LateTickEvent lateTickEvent,
            LoadingEvent loadingEvent,
            IObjectResolver resolver,
            ObjectResolverContainer resolverContainer,
            ClientWorldLoader worldLoader,
            WorldSetupService worldSetup,
            ClientPrefabProvider prefabProvider,
            ClientMusicRegistry musicRegistry
        )
        {
            this.tickEvent = tickEvent;
            this.fixedTickEvent = fixedTickEvent;
            this.lateTickEvent = lateTickEvent;
            this.loadingEvent = loadingEvent;
            this.worldLoader = worldLoader;
            this.worldSetup = worldSetup;
            this.prefabProvider = prefabProvider;
            this.musicRegistry = musicRegistry;
            resolverContainer.SetResolver(resolver);
            isInitialized = true;
        }

        public async void Start()
        {
            var musicPlayerPrefab = prefabProvider.MusicPlayerPrefab;
            if (musicPlayerPrefab != null)
            {
                var obj = UnityEngine.Object.Instantiate(musicPlayerPrefab);
                var player = obj.GetComponent<MusicPlayer>();
                player?.Initialize(musicRegistry, MusicType.World);
            }

            loadingEvent.Raise(true);
            var loadSucceeded = await worldLoader.LoadWorld();
            if (!loadSucceeded)
            {
                WorldLoadBootstrap.MarkClientFailed();
                return;
            }

            worldSetup.ExecuteSetup();
            WorldLoadBootstrap.MarkClientReady();
            loadingEvent.Raise(false);
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
