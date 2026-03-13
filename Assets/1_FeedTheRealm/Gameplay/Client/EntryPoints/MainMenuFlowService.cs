using Cysharp.Threading.Tasks;
using FTRShared.UI.AuthMenu;
using UnityEngine;

namespace FTR.Gameplay.Client.EntryPoints
{
    /// <summary>
    /// Service responsible for managing the flow of the client application, including authentication and main menu navigation.
    /// </summary>
    public class MainMenuFlowService
    {
        readonly GameObject loginPrefab;
        readonly GameObject signUpPrefab;
        readonly GameObject verifyCodePrefab;
        readonly GameObject worldFeedMenuPrefab;
        readonly GameObject navBarPrefab;
        readonly GameObject profileMenuPrefab;

        public MainMenuFlowService(
            GameObject loginPrefab,
            GameObject signUpPrefab,
            GameObject verifyCodePrefab,
            GameObject worldFeedMenuPrefab,
            GameObject navBarPrefab,
            GameObject profileMenuPrefab
        )
        {
            this.loginPrefab = loginPrefab;
            this.signUpPrefab = signUpPrefab;
            this.verifyCodePrefab = verifyCodePrefab;
            this.worldFeedMenuPrefab = worldFeedMenuPrefab;
            this.navBarPrefab = navBarPrefab;
            this.profileMenuPrefab = profileMenuPrefab;
        }

        public async UniTask ShowAuthFlow()
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
            var profileMenuObj = Object.Instantiate(profileMenuPrefab);
            profileMenuObj.SetActive(false);

            var navBarObj = Object.Instantiate(navBarPrefab);
            var navBarController = navBarObj.GetComponent<INavbarController>();
            if (navBarController != null)
                navBarController.SetProfileMenuInstance(profileMenuObj);

            var worldFeedMenuObj = Object.Instantiate(worldFeedMenuPrefab);
            var worldFeedMenu = worldFeedMenuObj.GetComponent<IMainMenuController>();

            var completionSource = new UniTaskCompletionSource();
            worldFeedMenu.OnNavigateToWorld += () => completionSource.TrySetResult();

            await completionSource.Task;

            Object.Destroy(profileMenuObj);
            Object.Destroy(navBarObj);
            Object.Destroy(worldFeedMenuObj);
        }
    }
}
