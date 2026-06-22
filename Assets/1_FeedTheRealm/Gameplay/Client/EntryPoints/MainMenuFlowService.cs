using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FTR.Core.Client.EventChannels.UI;
using FTR.Core.Common.Config;
using FTR.Core.Common.Enums;
using FTR.Gameplay.Client.Registry;
using FTRShared.UI.AuthMenu;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FTR.Gameplay.Client.EntryPoints
{
    public class MainMenuFlowService
    {
        readonly GameObject worldFeedMenuPrefab;
        readonly GameObject worldInfoMenuPrefab;
        readonly GameObject navBarPrefab;
        readonly GameObject profileMenuPrefab;
        readonly GameObject gemStorePrefab;
        readonly GameObject musicPlayerPrefab;
        readonly ClientMusicRegistry musicRegistry;
        private readonly ClientSoundFXRegistry soundFXRegistry;
        private readonly ISoundPlayer soundPlayer;
        readonly GameObject navBarSettingsPrefab;
        private readonly AuthFlowManager authFlowManager;
        private readonly API.PlayerService playerService;
        private readonly OnProfileCreatedEvent onProfileCreatedEvent;
        private readonly OnLogoutRequestedEvent onLogoutRequestedEvent;
        private readonly WorldInfoMenuHandle worldInfoMenuHandle;
        private readonly Config config;
        private readonly IObjectResolver resolver;
        private API.AuthService authService;
        private Session.Session session;
        private GameObject authBackgroundPrefab;
        private GameObject musicPlayerInstance;

        public MainMenuFlowService(
            GameObject worldFeedMenuPrefab,
            GameObject worldInfoMenuPrefab,
            GameObject navBarPrefab,
            GameObject profileMenuPrefab,
            GameObject gemStorePrefab,
            GameObject musicPlayerPrefab,
            ClientMusicRegistry musicRegistry,
            ClientSoundFXRegistry soundFXRegistry,
            ISoundPlayer soundPlayer,
            GameObject navBarSettingsPrefab,
            AuthFlowManager authFlowManager,
            API.PlayerService playerService,
            OnProfileCreatedEvent onProfileCreatedEvent,
            OnLogoutRequestedEvent onLogoutRequestedEvent,
            WorldInfoMenuHandle worldInfoMenuHandle,
            Config config,
            IObjectResolver resolver
        )
        {
            this.worldFeedMenuPrefab = worldFeedMenuPrefab;
            this.worldInfoMenuPrefab = worldInfoMenuPrefab;
            this.navBarPrefab = navBarPrefab;
            this.profileMenuPrefab = profileMenuPrefab;
            this.gemStorePrefab = gemStorePrefab;
            this.musicPlayerPrefab = musicPlayerPrefab;
            this.musicRegistry = musicRegistry;
            this.soundFXRegistry = soundFXRegistry;
            this.soundPlayer = soundPlayer;
            this.navBarSettingsPrefab = navBarSettingsPrefab;
            this.authFlowManager = authFlowManager;
            this.playerService = playerService;
            this.onProfileCreatedEvent = onProfileCreatedEvent;
            this.onLogoutRequestedEvent = onLogoutRequestedEvent;
            this.worldInfoMenuHandle = worldInfoMenuHandle;
            this.config = config;
            this.resolver = resolver;
        }

        public void InitializeMusicPlayer(MusicType type)
        {
            if (MusicPlayer.Instance != null)
            {
                Debug.Log("[MainMenuFlowService] MusicPlayer already exists.");
                return;
            }

            if (musicPlayerPrefab == null)
            {
                Debug.LogWarning("[MainMenuFlowService] MusicPlayer prefab is null!");
                return;
            }

            musicPlayerInstance = resolver.Instantiate(musicPlayerPrefab);
            var player = musicPlayerInstance.GetComponent<MusicPlayer>();
            player?.Initialize(musicRegistry, type);
        }

        public async UniTask DestroyMusicPlayerAsync(bool fadeOut = true)
        {
            if (MusicPlayer.Instance == null)
            {
                musicPlayerInstance = null;
                return;
            }

            if (fadeOut)
            {
                MusicPlayer.Instance.DestroyInstance(fadeOut: true);

                while (MusicPlayer.Instance != null)
                {
                    await UniTask.Yield();
                }
            }
            else
            {
                MusicPlayer.Instance?.DestroyInstance(fadeOut: false);
            }

            musicPlayerInstance = null;
        }

        public async UniTask ShowAuthFlow(
            API.AuthService authService,
            Session.Session session,
            GameObject authBackgroundPrefab
        )
        {
            this.authService = authService;
            this.session = session;
            this.authBackgroundPrefab = authBackgroundPrefab;

            await session.EnsureValidSession();
            (bool isSuccess, string _) = await authService.IsLogged();
            if (isSuccess)
                return;
            session.ClearSession();

            var authBackgroundObj = resolver.Instantiate(authBackgroundPrefab);
            var completionSource = new UniTaskCompletionSource();

            System.Action<string> onAuthComplete = (string _) => completionSource.TrySetResult();
            authFlowManager.OnAuthComplete += onAuthComplete;

            authFlowManager.HideCloseButton();
            authFlowManager.ShowAuthMenu();
            await completionSource.Task;

            authFlowManager.OnAuthComplete -= onAuthComplete;
            Object.Destroy(authBackgroundObj);
        }

        public async UniTask<bool> ShowMainMenuFlow()
        {
            // World Info menu
            var worldInfoMenuObj = Object.Instantiate(worldInfoMenuPrefab);
            worldInfoMenuObj.SetActive(false);
            resolver.InjectGameObject(worldInfoMenuObj);
            worldInfoMenuHandle.SetInstance(worldInfoMenuObj);

            // Home menu
            var worldFeedMenuObj = resolver.Instantiate(worldFeedMenuPrefab);
            resolver.InjectGameObject(worldFeedMenuObj);
            var worldFeedMenu = worldFeedMenuObj.GetComponent<IMainMenuController>();

            // Profile menu
            var profileMenuObj = resolver.Instantiate(profileMenuPrefab);
            profileMenuObj.SetActive(false);

            // Gem Store
            var gemStoreObj = Object.Instantiate(gemStorePrefab);
            gemStoreObj.SetActive(false);

            // Settings menu
            var navBarSettingsObj = resolver.Instantiate(navBarSettingsPrefab);
            navBarSettingsObj.SetActive(false);

            // NavBar — wire all instances
            var navBarObj = resolver.Instantiate(navBarPrefab);
            var navBarController = navBarObj.GetComponent<INavbarController>();
            if (navBarController != null)
            {
                navBarController.SetHomeMenuInstance(worldFeedMenuObj);
                navBarController.SetProfileMenuInstance(profileMenuObj);
                navBarController.SetGemStoreInstance(gemStoreObj);
                navBarController.SetNavBarSettingsInstance(navBarSettingsObj);
            }

            if (config.DisconnectionEvent == DisconnectionEvents.Unexpected)
                ToastNotification.Show(
                    "There was an error connecting to the server",
                    "error",
                    Color.red
                );
            config.DisconnectionEvent = DisconnectionEvents.None;

            await RedirectIfProfileRequired(worldFeedMenuObj, profileMenuObj, navBarController);

            var navigateSource = new UniTaskCompletionSource();
            var logoutSource = new UniTaskCompletionSource();

            System.Action onLogoutHandler = null;
            onLogoutHandler = () =>
            {
                session.ClearSession();
                logoutSource.TrySetResult();
            };
            onLogoutRequestedEvent.OnRaised += onLogoutHandler;

            worldFeedMenu.OnNavigateToWorld += () => navigateSource.TrySetResult();

            await UniTask.WhenAny(navigateSource.Task, logoutSource.Task);

            onLogoutRequestedEvent.OnRaised -= onLogoutHandler;

            bool wasLogout = logoutSource.Task.Status == UniTaskStatus.Succeeded;

            Object.Destroy(profileMenuObj);
            Object.Destroy(gemStoreObj);
            Object.Destroy(navBarSettingsObj);
            Object.Destroy(navBarObj);
            Object.Destroy(worldFeedMenuObj);
            Object.Destroy(worldInfoMenuObj);

            return wasLogout;
        }

        private async Task RedirectIfProfileRequired(
            GameObject worldFeedMenuObj,
            GameObject profileMenuObj,
            INavbarController navBarController
        )
        {
            bool needsProfileCreation = await CheckNeedsProfileCreation();

            if (needsProfileCreation && navBarController != null)
            {
                worldFeedMenuObj.SetActive(false);
                profileMenuObj.SetActive(true);

                navBarController.SetProfileLocked(true);

                System.Action onProfileCreated = null;
                onProfileCreated = () =>
                {
                    navBarController.SetProfileLocked(false);

                    profileMenuObj.SetActive(false);
                    worldFeedMenuObj.SetActive(true);

                    onProfileCreatedEvent.OnRaised -= onProfileCreated;
                };

                onProfileCreatedEvent.OnRaised += onProfileCreated;
            }
        }

        private async UniTask<bool> CheckNeedsProfileCreation()
        {
            if (playerService == null)
            {
                Debug.LogWarning(
                    "[MainMenuFlowService] PlayerService is null — skipping profile check."
                );
                return false;
            }

            var characterInfo = await playerService.GetCharacterInfoAsync();

            bool hasProfile =
                characterInfo != null && !string.IsNullOrWhiteSpace(characterInfo.character_name);

            return !hasProfile;
        }
    }
}
