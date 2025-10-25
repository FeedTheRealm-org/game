using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class NetworkSceneManager : NetworkBehaviour
{
    [System.Serializable]
    public class SceneReference
    {
#if UNITY_EDITOR
        [SerializeField] private Object sceneAsset;
#endif
        [SerializeField] private string sceneName;
        
        public string SceneName => sceneName;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (sceneAsset != null)
            {
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



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadGameScene()
    {
        if (gameScene == null || string.IsNullOrEmpty(gameScene.SceneName))
        {
            Debug.LogError("[NetworkSceneManager] Game scene reference is not set!");
            return;
        }
        
        if (NetworkManager.Singleton.IsServer)
        {
            //logger.Log($"Cargando escena del juego: {gameScene.SceneName}...", this);
            NetworkManager.SceneManager.LoadScene(gameScene.SceneName, LoadSceneMode.Single);
        }
        else
        {
            //logger.Log("Solo el servidor puede cargar escenas!", this, Logging.LogType.Error);
        }
    }

    [ServerRpc]
    public void LoadGameSceneServerRpc()
    {
        if (gameScene == null || string.IsNullOrEmpty(gameScene.SceneName))
        {
            Debug.LogError("[NetworkSceneManager] Game scene reference is not set!");
            return;
        }
        
        NetworkManager.SceneManager.LoadScene(gameScene.SceneName, LoadSceneMode.Single);
    }

    public void DisconnectAndReturnToMenu()
    {
        if (menuScene == null || string.IsNullOrEmpty(menuScene.SceneName))
        {
            Debug.LogError("[NetworkSceneManager] Menu scene reference is not set!");
            return;
        }
        
        //logger.Log($"Desconectando y volviendo al menú: {menuScene.SceneName}...", this, Logging.LogType.Info);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(menuScene.SceneName);
    }
    

        private void OnEnable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnClientConnected(ulong clientId)
    {
        //logger.Log($"Cliente {clientId} conectado. ¿Es servidor?: {NetworkManager.Singleton.IsServer}", this);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //logger.Log($"Escena cargada: {scene.name}. ¿Es servidor?: {NetworkManager.Singleton.IsServer}", this);
    } 
    
    public override void OnDestroy()
    {
        if (Instance == this)
        {
            // Unsubscribe from events
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            Instance = null;
        }
        
        base.OnDestroy();
    }
}