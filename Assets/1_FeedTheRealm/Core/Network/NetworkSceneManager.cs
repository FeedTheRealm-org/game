using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class NetworkSceneManager : Mirror.NetworkBehaviour {
    [System.Serializable]
    public class SceneReference {
#if UNITY_EDITOR
        [SerializeField] private Object sceneAsset;
#endif
        [SerializeField] private string sceneName;

        public string SceneName => sceneName;

#if UNITY_EDITOR
        private void OnValidate() {
            if (sceneAsset != null) {
                sceneName = sceneAsset.name;
            }
        }
#endif
    }

    public static NetworkSceneManager Instance;

    [Header("Scene References")]
    [SerializeField] private SceneReference gameScene;
    [SerializeField] private SceneReference menuScene;

    //[SerializeField] private Logging.Logger logger;



    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    public void LoadGameScene() {
        if (gameScene == null || string.IsNullOrEmpty(gameScene.SceneName)) {
            Debug.LogError("[NetworkSceneManager] Game scene reference is not set!");
            return;
        }

        if (isServer) {
            //logger.Log($"Loading game scene: {gameScene.SceneName}...", this);
            NetworkManager.singleton.ServerChangeScene(gameScene.SceneName);
        } else {
            //logger.Log("Only the server can load scenes!", this, Logging.LogType.Error);
        }
    }

    [Command]
    public void CmdLoadGameScene() {
        if (gameScene == null || string.IsNullOrEmpty(gameScene.SceneName)) {
            Debug.LogError("[NetworkSceneManager] Game scene reference is not set!");
            return;
        }

        NetworkManager.singleton.ServerChangeScene(gameScene.SceneName);
    }

    private void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        //logger.Log($"Scene loaded: {scene.name}. Is server?: {isServer}", this);
    }
}
