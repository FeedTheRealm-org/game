using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

public class ClientEntryPoint : IStartable
{
    private readonly SceneReference mainScene;

    public ClientEntryPoint(SceneReference worldScene)
    {
        this.mainScene = worldScene;
    }

    public async void Start()
    {
        ConfigureUnityForClient();
        await LoadMainScene();
    }

    void ConfigureUnityForClient()
    {
        // TODO: Load client config

        // Cap Update & LateUpdate TPS
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    async UniTask LoadMainScene()
    {
        await SceneManager.LoadSceneAsync(mainScene.SceneName, LoadSceneMode.Single);
    }
}
