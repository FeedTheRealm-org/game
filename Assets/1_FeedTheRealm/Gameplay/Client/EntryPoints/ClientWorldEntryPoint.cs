using System;
using System.Diagnostics;
using FeedTheRealm.Core.EventChannels.Setup;
using FeedTheRealm.Gameplay.Client.SceneSetup;
using FTR.Core.Client;
using FTR.Core.Client.EventChannels.Ticks;
using FTR.Core.Client.Input;
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
        private LoadingEvent loadingEvent;
        private bool isInitialized = false;

        private readonly ClientWorldLoader worldLoader;
        private readonly WorldSetupService worldSetup;
        private readonly ClientPrefabProvider prefabProvider;
        private ClientMusicRegistry musicRegistry;
        private CursorManager cursorManager;

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
            CursorManager cursorManager
        )
        {
            this.tickEvent = tickEvent;
            this.fixedTickEvent = fixedTickEvent;
            this.lateTickEvent = lateTickEvent;
            resolverContainer.SetResolver(resolver);
            this.worldLoader = worldLoader;
            this.worldSetup = worldSetup;
            this.loadingEvent = loadingEvent;
            this.prefabProvider = prefabProvider;
            this.musicRegistry = musicRegistry;
            this.cursorManager = cursorManager;
            isInitialized = true;
        }

        public async void Start()
        {
            cursorManager.ToggleCursorBlock(true);
            var musicPlayerPrefab = prefabProvider.MusicPlayerPrefab;
            if (musicPlayerPrefab != null)
            {
                var _object = UnityEngine.Object.Instantiate(musicPlayerPrefab);
                var player = _object.GetComponent<MusicPlayer>();
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
