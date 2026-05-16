using Cysharp.Threading.Tasks;
using FTRShared.UI.AuthMenu;
using UnityEngine;
using VContainer.Unity;

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
        readonly API.PlayerService playerService;
        readonly Session.Session session;
        readonly VContainer.IObjectResolver resolver;

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
            ClientMusicRegistry musicRegistry,
            API.PlayerService playerService,
            Session.Session session,
            VContainer.IObjectResolver resolver
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
            this.playerService = playerService;
            this.session = session;
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
            await session.EnsureValidSession();
            (bool isSuccess, string error) = await authService.IsLogged();
            if (isSuccess)
                return;

            session.ClearSession();

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
            var navBarObj = resolver.Instantiate(navBarPrefab);
            var navBarController = navBarObj.GetComponent<INavbarController>();
            if (navBarController != null)
            {
                navBarController.SetProfileMenuInstance(profileMenuObj);
                navBarController.SetGemStoreInstance(gemStoreObj);
            }

            bool hasCharacter = await CheckHasCharacterAsync();
            Debug.Log($"[MainMenuFlowService] Has character: {hasCharacter}");

            if (!hasCharacter)
            {
                await WaitForProfileCreationAsync(navBarController, profileMenuObj);

                Debug.Log(
                    "[MainMenuFlowService] Profile creation completed, proceeding to world feed."
                );
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

        private async UniTask WaitForProfileCreationAsync(
            INavbarController navBarController,
            GameObject profileMenuObj
        )
        {
            var profileDone = new UniTaskCompletionSource();

            var waiter = profileMenuObj.AddComponent<OnDisableNotifier>();
            waiter.OnDisabled += () => profileDone.TrySetResult();

            if (navBarController != null)
                navBarController.OpenProfile();
            else
                profileMenuObj.SetActive(true);

            await profileDone.Task;

            Object.Destroy(waiter);
        }

        private async UniTask<bool> CheckHasCharacterAsync()
        {
            if (playerService == null || session == null)
            {
                Debug.LogWarning(
                    "[MainMenuFlowService] PlayerService or Session is null, assuming no character."
                );
                return false;
            }

            try
            {
                var characterInfo = await playerService.GetCharacterInfoAsync();

                if (characterInfo == null)
                {
                    Debug.Log(
                        "[MainMenuFlowService] CharacterInfo is null (likely 404 or no character)."
                    );
                    return false;
                }

                bool hasName = !string.IsNullOrWhiteSpace(characterInfo.character_name);
                Debug.Log(
                    $"[MainMenuFlowService] Character name from API: '{characterInfo.character_name}'"
                );

                return hasName;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MainMenuFlowService] Error checking character: {ex.Message}");
                return false;
            }
        }
    }
}
