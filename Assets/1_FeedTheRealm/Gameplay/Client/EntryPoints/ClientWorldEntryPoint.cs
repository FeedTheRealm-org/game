using FeedTheRealm.Core.EventChannels.Setup;
using FeedTheRealm.Gameplay.Client.SceneSetup;
using FTR.Core.Client;
using FTR.Core.Client.EventChannels.Ticks;
using FTR.Core.Client.Managers;
using FTR.Core.Client.Settings;
using FTR.Core.Common.Scopes;
using FTR.Gameplay.Client.Loaders;
using FTRShared.Runtime.Core.Cache;
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
        private readonly SettingsManager settingsManager;
        private readonly CacheManager cacheManager;
        private CursorManager cursorManager;
        private bool isInitialized = false;
        private Logging.Logger logger;

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
            ClientMusicRegistry musicRegistry,
            CursorManager cursorManager,
            SettingsManager settingsManager,
            CacheManager cacheManager,
            Logging.Logger logger
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
            this.cursorManager = cursorManager;
            this.settingsManager = settingsManager;
            this.cacheManager = cacheManager;
            isInitialized = true;
            this.logger = logger;
        }

        public async void Start()
        {
            logger.Log("[ClientWorldEntryPoint] Starting client world entry point.");
            settingsManager.LoadSettings();
            settingsManager.ApplyDisplay();
            settingsManager.ApplyAudioListener();
            cacheManager.SetCachingEnabled(settingsManager.IsCachingEnabled);

            cursorManager.ToggleCursorBlock(true);
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
            await System.Threading.Tasks.Task.Delay(2000);
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
