using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

public class ServerEntryPoint : MonoBehaviour
{
    [SerializeField]
    private SceneReference worldScene;

    async void Awake()
    {
        DontDestroyOnLoad(gameObject);

        ConfigureUnityForServer();

        await LoadWorldScene();
    }

    void ConfigureUnityForServer()
    {
        // Turn off automatic physics simulation (FixedUpdate not called anymore)
        Physics.simulationMode = SimulationMode.Script;

        // Cap Update & LateUpdate TPS
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    async UniTask LoadWorldScene()
    {
        await SceneManager.LoadSceneAsync(worldScene.SceneName, LoadSceneMode.Single);
    }
}
