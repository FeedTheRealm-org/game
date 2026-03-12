using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

public class ClientEntryPoint : IStartable
{
    private readonly SceneReference mainScene;
    private readonly Session.Session session;
    private readonly GameObject loginPrefab;
    private readonly GameObject signUpPrefab;
    private readonly GameObject verifyCodePrefab;
    private readonly GameObject worldFeedMenuPrefab;
    private readonly GameObject navBarPrefab;
    private readonly GameObject profileMenuPrefab;

    public ClientEntryPoint(
        SceneReference mainScene,
        Session.Session session,
        GameObject loginPrefab,
        GameObject signUpPrefab,
        GameObject verifyCodePrefab,
        GameObject worldFeedMenuPrefab,
        GameObject navBarPrefab,
        GameObject profileMenuPrefab
    )
    {
        this.mainScene = mainScene;
        this.session = session;
        this.loginPrefab = loginPrefab;
        this.signUpPrefab = signUpPrefab;
        this.verifyCodePrefab = verifyCodePrefab;
        this.worldFeedMenuPrefab = worldFeedMenuPrefab;
        this.navBarPrefab = navBarPrefab;
        this.profileMenuPrefab = profileMenuPrefab;
    }

    public async void Start()
    {
        ConfigureUnityForClient();

        await ShowAuthFlow();
        await ShowMainMenuFlow();
        await LoadMainScene();
    }

    void ConfigureUnityForClient()
    {
        // TODO: Load client config

        // Cap Update & LateUpdate TPS
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    async UniTask ShowAuthFlow()
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

    async UniTask ShowMainMenuFlow()
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

    async UniTask LoadMainScene()
    {
        await SceneManager.LoadSceneAsync(mainScene.SceneName, LoadSceneMode.Single);
    }
}
