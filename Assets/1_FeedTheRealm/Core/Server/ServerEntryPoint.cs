using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

public class ServerEntryPoint : IStartable
{
    private readonly SceneReference mainScene;

    public ServerEntryPoint(SceneReference worldScene)
    {
        this.mainScene = worldScene;
    }

    public async void Start()
    {
        ServerMessage();
        ConfigureUnityForServer();
        await LoadMainScene();
    }

    void ConfigureUnityForServer()
    {
        // Turn off automatic physics simulation (FixedUpdate not called anymore)
        Physics.simulationMode = SimulationMode.Script;

        // Cap Update & LateUpdate TPS
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    async UniTask LoadMainScene()
    {
        await SceneManager.LoadSceneAsync(mainScene.SceneName, LoadSceneMode.Single);
    }

    private void ServerMessage()
    {
        Debug.Log(
            "------------------------------------------------------------------------------------------------\n"
                + "________________________________   ._.    __________________________________   _________________________\n"
                + "\\_   _____/\\__    ___/\\______   \\  | |   /   _____/\\_   _____/\\______   \\   \\ /   /\\_   _____/\\______   \\\n"
                + " |    __)    |    |    |       _/  |_|   \\_____  \\  |    __)_  |       _/\\   Y   /  |    __)_  |       _/\n"
                + " |     \\     |    |    |    |   \\  |-|   /        \\ |        \\ |    |   \\ \\     /   |        \\ |    |   \\\n"
                + " \\___  /     |____|    |____|_  /  | |  /_______  //_______  / |____|_  /  \\___/   /_______  / |____|_  /\n"
                + "     \\/                       \\/   |_|          \\/         \\/         \\/                   \\/         \\/ \n"
                + "------------------------------------------------------------------------------------------------\n"
        );
    }
}
