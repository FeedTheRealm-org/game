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

        public MainMenuFlowService(
            GameObject loginPrefab,
            GameObject signUpPrefab,
            GameObject verifyCodePrefab,
            GameObject worldFeedMenuPrefab,
            GameObject navBarPrefab,
            GameObject profileMenuPrefab,
            GameObject gemStorePrefab
        )
        {
            this.loginPrefab = loginPrefab;
            this.signUpPrefab = signUpPrefab;
            this.verifyCodePrefab = verifyCodePrefab;
            this.worldFeedMenuPrefab = worldFeedMenuPrefab;
            this.navBarPrefab = navBarPrefab;
            this.profileMenuPrefab = profileMenuPrefab;
            this.gemStorePrefab = gemStorePrefab;
        }

        public async UniTask ShowAuthFlow(API.AuthService authService, Session.Session session)
        {
            var loginObj = Object.Instantiate(loginPrefab);
            var signUpObj = Object.Instantiate(signUpPrefab);
            var verifyCodeObj = Object.Instantiate(verifyCodePrefab);

            var authFlow = new AuthFlowManager(
                loginObj,
                signUpObj,
                verifyCodeObj,
                authService,
                session
            );

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
