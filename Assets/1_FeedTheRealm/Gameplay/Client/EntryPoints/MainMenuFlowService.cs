using Cysharp.Threading.Tasks;
using FTRShared.UI.AuthMenu;
using UnityEngine;

namespace FTR.Gameplay.Client.EntryPoints
{
    public class MainMenuFlowService
    {
        readonly GameObject loginPrefab;
        readonly GameObject signUpPrefab;
        readonly GameObject verifyCodePrefab;
        readonly GameObject worldFeedMenuPrefab;
        readonly GameObject navBarPrefab;
        readonly GameObject profileMenuPrefab;
        readonly GameObject gemStorePrefab;
        readonly GameObject musicPlayerPrefab;
        readonly ClientMusicRegistry musicRegistry;

        private GameObject musicPlayerInstance;

        public MainMenuFlowService(
            GameObject loginPrefab,
            GameObject signUpPrefab,
            GameObject verifyCodePrefab,
            GameObject worldFeedMenuPrefab,
            GameObject navBarPrefab,
            GameObject profileMenuPrefab,
            GameObject gemStorePrefab,
            GameObject musicPlayerPrefab,
            ClientMusicRegistry musicRegistry
        )
        {
            this.loginPrefab = loginPrefab;
            this.signUpPrefab = signUpPrefab;
            this.verifyCodePrefab = verifyCodePrefab;
            this.worldFeedMenuPrefab = worldFeedMenuPrefab;
            this.navBarPrefab = navBarPrefab;
            this.profileMenuPrefab = profileMenuPrefab;
            this.gemStorePrefab = gemStorePrefab;
            this.musicPlayerPrefab = musicPlayerPrefab;
            this.musicRegistry = musicRegistry;
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

            musicPlayerInstance = Object.Instantiate(musicPlayerPrefab);
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

        public async UniTask ShowAuthFlow(API.AuthService authService, Session.Session session)
        {
            var loginObj = Object.Instantiate(loginPrefab);
            var signUpObj = Object.Instantiate(signUpPrefab);
            var verifyCodeObj = Object.Instantiate(verifyCodePrefab);

            var authFlow = new AuthFlowManager(loginObj, signUpObj, verifyCodeObj);
            var completionSource = new UniTaskCompletionSource();
            authFlow.OnAuthComplete += () => completionSource.TrySetResult();
            authFlow.Initialize();

            await completionSource.Task;

            authFlow.Destroy();
        }

        public async UniTask ShowMainMenuFlow()
        {
            // Profile menu
            var profileMenuObj = Object.Instantiate(profileMenuPrefab);
            profileMenuObj.SetActive(false);

            // Gem Store
            var gemStoreObj = Object.Instantiate(gemStorePrefab);
            gemStoreObj.SetActive(false);

            // NavBar — wire both instances
            var navBarObj = Object.Instantiate(navBarPrefab);
            var navBarController = navBarObj.GetComponent<INavbarController>();
            if (navBarController != null)
            {
                navBarController.SetProfileMenuInstance(profileMenuObj);
                navBarController.SetGemStoreInstance(gemStoreObj);
            }

            var worldFeedMenuObj = Object.Instantiate(worldFeedMenuPrefab);
            var worldFeedMenu = worldFeedMenuObj.GetComponent<IMainMenuController>();

            var completionSource = new UniTaskCompletionSource();
            worldFeedMenu.OnNavigateToWorld += () => completionSource.TrySetResult();

            await completionSource.Task;

            Object.Destroy(profileMenuObj);
            Object.Destroy(gemStoreObj);
            Object.Destroy(navBarObj);
            Object.Destroy(worldFeedMenuObj);
        }
    }
}
